# Bitcoin Contracts

A console based toolkit for developing smart contracts on the Bitcoin and Litecoin networks in .NET using the NBitcoin libraries.

Note: Code is for Bitcoin, can be converted to Litecoin by swapping out NBitcoin for NBitcoin.Litecoin.

### References

##### Terminal Commands to find TestNet nodes:
```
for i in testnet - seed.bitcoin.jonasschnelli.ch \
	seed.tbtc.petertodd.org \
	testnet - seed.bluematt.me \
	testnet - seed.bitcoin.schildbach.de
do
	nslookup $i 2 > &1 | grep Address | cut - d' ' - f2
done
```

##### GitHub
https://github.com/MetacoSA/NBitcoin
https://github.com/MetacoSA/NBitcoin.Litecoin
https://github.com/MetacoSA/QBitNinja

##### Nuget
https://www.nuget.org/packages/nbitcoin
https://www.nuget.org/packages/QBitninja.Client

##### Articles
https://github.com/ProgrammingBlockchain/ProgrammingBlockchainCodeExamples
https://www.codeproject.com/Articles/835098/NBitcoin-Build-Them-All