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

        //Create Bitcoin address and HD seed phrase
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

        //Open wallets for contract examples to transact with
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

        #region Contracts

        /// <summary>
        /// Simple transaction that is locked for x amount of hours before being processed
        /// </summary>
        public static void SimpleTimeLockContract()
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

            //Hours to wait until transaction is allowed to process
            int timeLockHours = 5;

            //Console.WriteLine(privKey + "|" + secret.GetAddress());

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


            // The amount we are sending to the contractee (minus the mining fee)
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

            transaction.LockTime = new LockTime(DateTime.Now.AddHours(timeLockHours));

            transaction.Sign(secret, false);

            

            Console.WriteLine("Timelock contract signed and broadcast.");


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


            Console.WriteLine(transaction.ToString());
            /*
            //Broadcast using QBit server:
            var client = new QBitNinjaClient(_network);
            BroadcastResponse broadcastResponse = client.Broadcast(transaction).Result;


            Console.WriteLine("Transaction ID: " + transaction.GetHash().ToString());
            Console.WriteLine();

            if(broadcastResponse.Success)
            {
                Console.WriteLine("Broadcast succeeded!");
            }
            else
            {
                Console.WriteLine("Broadcase Error!");
                Console.WriteLine();
                Console.WriteLine(broadcastResponse.Error.Reason);
            }
            */
        }

        #endregion

    }
}
