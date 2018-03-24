using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NBitcoin; //<-- Nuget Package (Client library. Implements all relevant Bitcoin Improvement Proposals (BIPs)
using QBitNinja.Client; //<-- Client for API (Can also create your own node server with API to broadcast to Test & Main Networks)
using QBitNinja.Client.Models;
using System.Linq; //<--- Required for .ToArray() calls on NBitcoin Library
using System.Security.Cryptography; //<--Required for hash comparisons
using System.Threading;
using NBitcoin.Protocol; //<-- Required for NBicoin Protocol Node Messaging

namespace BitcoinContracts
{
    class Program
    {
        public static NBitcoin.Network _network = Network.TestNet;  //<--- Switch to Network.Main when in production

        //Note: Contractor wallet must have a testnet bitcoin balance
        public static string _contractorWif = "tprv8ZgxMBicQKsPdfmxCxVLVsUaKeGrUhAU1sfDj3Cg86Te2oXj8ZXgJXJMDiPUuYgQhvmd5MjiVaAaapxGgZFjUdkNaU3ZhVzK2nv1dy8gwMj"; //<---Pre-Generated using CreateAddress function
        public static string _contracteeWif = "tprv8ZgxMBicQKsPf2J9r2zz92JGjQnp1muzdU8XxELgnd5m5hNuchUNsSEC79nZ86QsT3YMfdVtC6fJW4ZBLRn4YEe8wndL8vtL1g2xyLaeRqJ"; //<---Pre-Generated using CreateAddress function

        public static ExtKey _contractorHdRoot;
        public static ExtKey _contracteeHdRoot;

        static void Main(string[] args)
        {
            OpenWallets();
            Console.WriteLine();


            SimpleTimeLockContract();
            Console.WriteLine();

            Console.WriteLine();
            Console.WriteLine("...Press any key to exit.");
            Console.ReadLine();
        }

        #region Setup & Utilities

        /// <summary>
        /// Create Bitcoin address and HD seed phrase
        /// </summary>
        public static void CreateAddress()
        {
            Mnemonic mnemo = new Mnemonic(Wordlist.English, WordCount.Twelve);
            ExtKey hdroot = mnemo.DeriveExtKey();

            var wif = hdroot.GetWif(_network);
            var defualtAddress = hdroot.Derive(new KeyPath("m/44'/0'/0'/0/0"));

            #region Alt

            //Alternate method of derivation:
            //var defualtAddress = hdroot.Derive(44, true).Derive(0, true).Derive(0, true).Derive(0).Derive(0);

            #endregion

            Console.WriteLine("Public Address: " + defualtAddress.ScriptPubKey.GetDestinationAddress(_network));
            Console.WriteLine("WIF: " + defualtAddress.PrivateKey);
            Console.WriteLine("Seed Phrase: " + mnemo);
            

        }

        /// <summary>
        /// Open wallets for contract examples to transact with
        /// </summary>
        public static void OpenWallets()
        {
            _contractorHdRoot = ExtKey.Parse(_contractorWif, _network);
            var contractorPublicAddress = _contractorHdRoot.Derive(new KeyPath("m/44'/0'/0'/0/0"));
            Console.WriteLine("Contractor wallet open. (" + contractorPublicAddress.ScriptPubKey.GetDestinationAddress(_network) + ")");

            _contracteeHdRoot = ExtKey.Parse(_contracteeWif, _network);
            var contracteePublicAddress = _contracteeHdRoot.Derive(new KeyPath("m/44'/0'/0'/0/0"));
            Console.WriteLine("Contractee wallet open. (" + contracteePublicAddress.ScriptPubKey.GetDestinationAddress(_network) + ")");

        }





        #endregion

        #region Example Scripts

        /// <summary>
        /// Work with previous transactions to build out available coin data
        /// </summary>
        public static void WorkingWithCoins()
        {
            //Create a client for our QBitNinja API calls
            var client = new QBitNinjaClient(_network);


            //Get the unspent outputs/coins from the last transaction for the contractor that can be used
            //GetTransactionResponse transactionResponse = client.GetTransaction(uint256.Parse("3fd79e0843b8f834737abfb7c39a65bcab164d3229a1da3b71224989c2ab048b")).Result;
            GetTransactionResponse transactionResponse = client.GetTransaction(uint256.Parse("26b3a2ff92a918ad8c46c2da7dbf4c27c7a12bd24655c9d12f03d0f18edcede6")).Result;



            //Generate a list of receive coins from the transaction
            List<ICoin> receivedCoins = transactionResponse.ReceivedCoins;
            List<ICoin> spentCoins = transactionResponse.SpentCoins;

            Console.WriteLine("--------------- RECEIVED COINS------------------");
            Console.WriteLine();
            foreach (var coin in receivedCoins)
            {
                Money amount = (Money)coin.Amount;
                var paymentScript = coin.TxOut.ScriptPubKey;
                var address = paymentScript.GetDestinationAddress(_network);

                Console.WriteLine("Amount: " + amount.ToDecimal(MoneyUnit.BTC) + " | " + paymentScript + " | " + address);
                Console.WriteLine();

            }

            Console.WriteLine();

            Console.WriteLine("--------------- SPENT COINS ------------------");
            Console.WriteLine();
            foreach (var coin in spentCoins)
            {
                Money amount = (Money)coin.Amount;
                var paymentScript = coin.TxOut.ScriptPubKey;
                var address = paymentScript.GetDestinationAddress(_network);

                Console.WriteLine("Amount: " + amount.ToDecimal(MoneyUnit.BTC) + " | " + paymentScript + " | " + address);
                Console.WriteLine();

            }
        }


        /// <summary>
        /// Simple transaction that is locked for x amount of hours before being processed
        /// </summary>
        public static void SendSimpleTransation()
        {

            var privKey = (_contractorHdRoot.Derive(new KeyPath("m/44'/0'/0'/0/0"))).PrivateKey.GetWif(_network);

            var secret = new BitcoinSecret(privKey.ToString(), _network);
            var key = secret.PrivateKey;
            var changeAddress = secret.GetAddress().ScriptPubKey;

            string previousOutputTxId = "26b3a2ff92a918ad8c46c2da7dbf4c27c7a12bd24655c9d12f03d0f18edcede6";
            int unspentOutputIndex = 0; //<-- The index of the unspent output from the previous transaction

            string contracteePublicAddress = "n4bEeKENL9rED2cG31TjSNzPs6T15TCq96";

            int feeAmount = 2000;
            int paymentAmount = 1002000;
            int changeAmount = 51993000;

            #region Inputs

            var transaction = new Transaction();
            transaction.Inputs.Add(new TxIn()
            {
                PrevOut = new OutPoint(new uint256(previousOutputTxId), unspentOutputIndex), //<-- The output we are spending from
                ScriptSig = secret.GetAddress().ScriptPubKey
            });

            #endregion

            #region Outputs

            var destination = BitcoinAddress.Create(contracteePublicAddress);
            Money fee = Money.Satoshis(feeAmount);


            transaction.Outputs.Add(new TxOut()
            {
                ScriptPubKey = destination.ScriptPubKey,
                Value = Money.Satoshis(paymentAmount)
            });


            // The change address for the contractor
            transaction.Outputs.Add(new TxOut()
            {
                ScriptPubKey = changeAddress,
                Value = Money.Satoshis(changeAmount)
            });

            #endregion

            transaction.Sign(secret, false);


            Console.WriteLine("Transaction signed and broadcast.");


            #region Broadcast transaction using NBitcoin rather than QBitServerClient

            /*
            //Use Bitnodes to find a node to connect to: https://bitnodes.earn.com/  |  https://bitnodes.earn.com/nodes/
            //For Testnet you may have to find a faucet provider that also provides node information. 
            var node = NBitcoin.Protocol.Node.Connect(_network, "185.28.76.179:18333");
            node.VersionHandshake();

            var payload = NBitcoin.Protocol.Payload()
            node.SendMessage(new NBitcoin.Protocol.Payload().);

            Thread.Sleep(4000);
            node.Disconnect();
            */

            #endregion


            //Broadcast using QBit server:
            var client = new QBitNinjaClient(_network);
            BroadcastResponse broadcastResponse = client.Broadcast(transaction).Result;


            Console.WriteLine("Transaction ID: " + transaction.GetHash().ToString());
            Console.WriteLine();

            if (broadcastResponse.Success)
            {
                Console.WriteLine("Broadcast succeeded!");
            }
            else
            {
                Console.WriteLine("Broadcase Error!");
                Console.WriteLine();
                Console.WriteLine(broadcastResponse.Error.Reason);
            }

        }


        /// <summary>
        /// Send a message of 80 bytes or less
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static bool SendMessage(string message)
        {
            var messageBytes = Encoding.UTF8.GetBytes(message);

            //Create a client for our QBitNinja API calls
            var client = new QBitNinjaClient(_network);

            var privateKey = (_contractorHdRoot.Derive(new KeyPath("m/44'/0'/0'/0/0"))).PrivateKey.GetWif(_network);
            var secret = new BitcoinSecret(privateKey.ToString(), _network);

            var changeAddress = secret.GetAddress().ScriptPubKey;
            int changeAmount = 166712111;

            string previousOutputTxId = "af6836c0ced8dad52ae7a02d5f81185a2cd5c369e11e368b35d2bc2bea960e2e";
            int unspentOutputIndex = 0; //<-- The index of the unspent output from the previous transaction


            try
            {

                var transaction = new Transaction();

                //Previous input with spendable assets
                transaction.Inputs.Add(new TxIn()
                {
                    PrevOut = new OutPoint(new uint256(previousOutputTxId), unspentOutputIndex), //<-- The output we are spending from
                    ScriptSig = secret.GetAddress().ScriptPubKey
                });

                //The message
                transaction.Outputs.Add(new TxOut()
                {
                    Value = Money.Zero,
                    ScriptPubKey = TxNullDataTemplate.Instance.GenerateScriptPubKey(messageBytes)
                });

                // The change address for the contractor
                transaction.Outputs.Add(new TxOut()
                {
                    ScriptPubKey = changeAddress,
                    Value = Money.Satoshis(changeAmount)
                });


                transaction.Sign(secret, false);


                Console.WriteLine("Transaction signed and broadcast.");

                BroadcastResponse broadcastResponse = client.Broadcast(transaction).Result;


                Console.WriteLine("Transaction ID: " + transaction.GetHash().ToString());
                Console.WriteLine();

                if (broadcastResponse.Success)
                {
                    Console.WriteLine("Broadcast succeeded!");
                }
                else
                {
                    Console.WriteLine("Broadcase Error!");
                    Console.WriteLine();
                    Console.WriteLine(broadcastResponse.Error.Reason);
                }


            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }



            Console.WriteLine();
            return true;



        }


        #endregion

        #region Contracts

        /// <summary>
        /// Simple transaction that is locked for x amount of hours before being processed
        /// </summary>
        public static bool SimpleTimeLockContract()
        {
            //Create a client for our QBitNinja API calls
            var client = new QBitNinjaClient(_network);

            var contractorPrivateKey = (_contractorHdRoot.Derive(new KeyPath("m/44'/0'/0'/0/0"))).PrivateKey.GetWif(_network);
            var contractorSecret = new BitcoinSecret(contractorPrivateKey.ToString(), _network);

            var contracteeAddress = BitcoinAddress.Create("n4bEeKENL9rED2cG31TjSNzPs6T15TCq96", _network);

            //Calculate all payments and fees
            var contracteePaymentAmount = new Money(0.1m, MoneyUnit.BTC);
            var minerFee = new Money(0.00007m, MoneyUnit.BTC);
            

            int lockTimeHours = 5; // <-- Hours to wait until transaction is allowed to process

            //Collect the spendable coins from a previous transction
            GetTransactionResponse transactionResponse = client.GetTransaction(uint256.Parse("f9f650416c5c3b1ebb6137734f0daf31f91744398eab1a88ba806cf10d7afb6c")).Result;
            List<ICoin> receivedCoins = transactionResponse.ReceivedCoins;

            OutPoint outPointToSpend = null;

            Console.WriteLine("------ Available Coins ------");
            Console.WriteLine();

            foreach (var coin in receivedCoins)
            {
                if(coin.TxOut.ScriptPubKey == contractorPrivateKey.ScriptPubKey)
                {
                    outPointToSpend = coin.Outpoint;

                    Money amount = (Money)coin.Amount;
                    var paymentScript = coin.TxOut.ScriptPubKey;
                    var address = paymentScript.GetDestinationAddress(_network);

                    Console.WriteLine("Amount: " + amount.ToDecimal(MoneyUnit.BTC) + " | " + paymentScript + " | " + address);
                    Console.WriteLine();
                }


            }
           

            if (outPointToSpend == null)
            {
                Console.WriteLine("Transaction does not contain our ScriptPubKey!");
                return false;
            }

            Console.WriteLine("-----------------------------");




            //calculate change amount (not actually used, TransactionBuilder will handle for us):
            var txInAmount = (Money)receivedCoins[(int)outPointToSpend.N].Amount;
            var changeAmount = txInAmount - contracteePaymentAmount - minerFee;


            #region Console Output (Payment Details)

            Console.WriteLine();
            Console.WriteLine();
            Console.WriteLine("------ Payment Details ------");
            Console.WriteLine();

            Console.WriteLine("Total available: " + txInAmount.ToString() + " btc");
            Console.WriteLine();
            Console.WriteLine("Total payments: " + contracteePaymentAmount.ToString() + " btc");
            Console.WriteLine();
            Console.WriteLine("Total fees: " + minerFee.ToString() + " btc");
            Console.WriteLine();
            Console.WriteLine("Change: " + changeAmount.ToString() + " btc");
            Console.WriteLine();


            Console.WriteLine("-----------------------------");

            Console.WriteLine("");
            Console.WriteLine("Press any key to create, sign and broadcast transaction...");
            Console.ReadLine();

            #endregion



            try
            {
                //Now we can build a transaction where we send a timelock payment to the contractor
                var txBuilder = new TransactionBuilder();
                var tx = txBuilder
                    .AddCoins(receivedCoins)
                    .AddKeys(contractorSecret.PrivateKey)
                    .Send(contracteeAddress.ScriptPubKey, contracteePaymentAmount)
                    .SendFees(minerFee)
                    .SetChange(contractorSecret.GetAddress())
                    .SetLockTime(new LockTime(DateTimeOffset.Now.AddHours(lockTimeHours)))
                    .BuildTransaction(true);


                if (txBuilder.Verify(tx))
                {
                    Console.WriteLine("Timelock contract created, and signed.");

                    //Console.WriteLine();
                    //Console.WriteLine(tx.ToString()); //<-- Print out entire transaction as JSON

                    #region Broadcast transaction using NBitcoin with node connection

                    /*
                    //Use Bitnodes to find a node to connect to: https://bitnodes.earn.com/  |  https://bitnodes.earn.com/nodes/
                    //For Testnet you may have to find a faucet provider that also provides node information. 


                    var node = NBitcoin.Protocol.Node.Connect(_network, "52.10.6.141:18333"); //<-- ReadMe has Terminal commands for looking up available Testnet Nodes. All Testnodes use :18333
                    node.VersionHandshake();

                   // var payload = NBitcoin.Protocol.Payload(tx);

                    //inform the server
                    node.SendMessage(new InvPayload(tx));
                    Thread.Sleep(1000);

                    //send the transaction
                    node.SendMessage(new TxPayload(tx));
                    Thread.Sleep(5000);

                    node.Disconnect();

                    Console.WriteLine("Transaction ID: " + tx.GetHash().ToString());
                    Console.WriteLine();

                    */

                    #endregion

                    #region Broadcast transaction using QbitServerClient


                    //Broadcast using QBit server:
                    BroadcastResponse broadcastResponse = client.Broadcast(tx).Result;


                    Console.WriteLine("Transaction ID: " + tx.GetHash().ToString());
                    Console.WriteLine();

                    if (broadcastResponse.Success)
                    {
                        Console.WriteLine("Broadcast succeeded!");
                    }
                    else
                    {
                        Console.WriteLine("Broadcase Error!");
                        Console.WriteLine();
                        Console.WriteLine(broadcastResponse.Error.Reason);
                    }



                    #endregion
                }
                else
                {
                    Console.WriteLine("Timelock contract has some issues.");
                }
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
            }



            Console.WriteLine();
            return true;

            
        }

    #endregion
 
     }
}
