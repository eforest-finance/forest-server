using System.Collections.Generic;

namespace NFTMarketServer.Dealer.Options;

public class ChainOption
{
    // how many seconds for waiting transaction results
    public int InvokeExpireSeconds { get; set; } = 60;

    // how many milliSeconds delay for query transaction results
    public int QueryTransactionDelayMillis { get; set; } = 1000;

    public int QueryPendingTxSecondsAgo { get; set; } = 120;
    
    // chainId => node url
    public Dictionary<string, string> ChainNode { get; set; } = new();

    // account name => account item
    public Dictionary<string, AccountOptionItem> AccountOption { get; set; } = new();

    // chainId => contractName => contractAddress
    public Dictionary<string, Dictionary<string, string>> ContractAddress { get; set; } = new();
}

public class AccountOptionItem
{
    public string Address { get; set; }
    public string PrivateKey { get; set; }
    public string PublicKey { get; set; }
}