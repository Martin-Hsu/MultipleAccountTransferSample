using System;
using System.Threading.Tasks;
using Nethereum.Geth;
using System.Collections.Generic;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using System.Numerics;
using Nethereum.RPC.TransactionReceipts;
using Nethereum.Web3.Accounts.Managed;
using System.IO;

namespace AccountTransfer
{
    class Program
    {
        static void Main(string[] args)
        {
            new TransferDemoService().ShouldBeAbleToTransferBetweenAccountsUsingManagedAccount().Wait();
            //new TransferDemoService().TransferDemo().Wait();
        }
    }

    public class TransferDemoService
    {
        public async Task ShouldBeAbleToTransferBetweenAccountsUsingManagedAccount()
        {
            //var senderAddress = "0x743558527352b175e5f9c303ac00c04b9b32518d";
            var addressTo = "0x9c616e970cd7c2eb2c89b25b2a9dc07309eb0239";
            //var password = "small123";

           
            var password = "small123";
            //var accountFilePath = @"‪C:\a";
            string json = "{\"address\":\"743558527352b175e5f9c303ac00c04b9b32518d\",\"crypto\":{\"cipher\":\"aes-128-ctr\",\"ciphertext\":\"1256e621ba5e8c61c55416ff4555787e5e50588228cc5259ea4c832058c96054\",\"cipherparams\":{\"iv\":\"cc20ac14d92ea696512c3dcd43fe7b84\"},\"kdf\":\"scrypt\",\"kdfparams\":{\"dklen\":32,\"n\":262144,\"p\":1,\"r\":8,\"salt\":\"30f124583315b9d52b858f770e39db9e2b459c4807d8245289c820fdb8c0d8b5\"},\"mac\":\"26744e92ff8ced4efd73681730eb837c33ab64b689a5bc8ab365e2d70c4227ac\"},\"id\":\"9cb6f67f-6789-4987-922a-03ed1b71b4a2\",\"version\":3}";
            var account = Nethereum.Web3.Accounts.Account.LoadFromKeyStore(json, password);
            //var g = new Nethereum.Web3.Accounts.Account();
            var web3 = new Nethereum.Web3.Web3(account);
            web3.TransactionManager.DefaultGasPrice = new BigInteger(5000000000);
            // A managed account is an account which is maintained by the client (Geth / Parity)
            //var account = new ManagedAccount(senderAddress, password);
            //var web3 = new Web3(account);

            //var web3 = new Web3();
            //await web3.Personal.UnlockAccount.SendRequestAsync(senderAddress, password, 60000);
            //The transaction receipt polling service is a simple utility service to poll for receipts until mined
            var transactionPolling = web3.TransactionManager.TransactionReceiptService;
            
            var myBalance = await web3.Eth.GetBalance.SendRequestAsync(account.Address);
            var currentBalance = await web3.Eth.GetBalance.SendRequestAsync(addressTo);
            //assumed client is mining already
            //var web3Geth = new Web3Geth();
            //await web3Geth.Miner.Start.SendRequestAsync(2);
            //When sending the transaction using the transaction manager for a managed account, personal_sendTransaction is used.
            try
            {
                var transactionReceipt = await transactionPolling.SendRequestAsync(() =>
                    web3.TransactionManager.SendTransactionAsync(account.Address, addressTo, new HexBigInteger(1))
                );
            }
            catch (Exception ex)
            {
                if (ex.Message != "unknown transaction") {
                    throw ex;
                }
            }
            

            var newBalance = await web3.Eth.GetBalance.SendRequestAsync(addressTo);

        }

        public async Task TransferDemo()
        {
            var address = "0x743558527352b175e5f9c303ac00c04b9b32518d";
            var receiver = "0x02131c4e63fc13d68e490c30e5e265b4f07115f2";
            var defaultPassword = "small123";
            var web3 = new Web3Geth();
            var accountBalance = await web3.Eth.GetBalance.SendRequestAsync(address);
            var accounts = new List<string>();
            await web3.Personal.UnlockAccount.SendRequestAsync(address, defaultPassword, 60000);
           
            //var newAccount = await web3.Personal.NewAccount.SendRequestAsync(defaultPassword);
            accounts.Add(receiver);
            var coinBase = await web3.Eth.CoinBase.SendRequestAsync();
            await web3.Miner.Start.SendRequestAsync(4);
            var initialBalance = await web3.Eth.GetBalance.SendRequestAsync(address);
            //2 Ether
            var transactionReceiptsInitial = await TransferEqualAmounts(web3, address, Web3.Convert.ToWei(0.02), accounts.ToArray());

            var mainAccountBalance = await web3.Eth.GetBalance.SendRequestAsync(address);
           
            var account1Balance = await web3.Eth.GetBalance.SendRequestAsync(accounts[0]);

            //await web3.Miner.Stop.SendRequestAsync();
        }

        public async Task<List<TransactionReceipt>> TransferEqualAmounts(Web3 web3, string from, BigInteger amount,
            params string[] toAdresses)
        {
            //web3.TransactionManager.DefaultGas = new BigInteger(800000);
            //web3.TransactionManager.DefaultGasPrice = new BigInteger(3000000000);

            var transfers = new List<Func<Task<string>>>();

          
            foreach (var to in toAdresses)
            {
                transfers.Add(() =>
                    web3.Eth.TransactionManager.SendTransactionAsync(new TransactionInput()
                    {
                        From = from,
                        To = to,
                        Value = new HexBigInteger(amount)
                        , Gas = new HexBigInteger(900000)
                //, GasPrice = new HexBigInteger(2000000000)
            }));
            }
            var pollingService = new TransactionReceiptPollingService(web3.TransactionManager);
            return await pollingService.SendRequestsAsync(transfers);
        }

    }
}