using System.Collections.Generic;

namespace NFTMarketServer.ThirdToken.Strategy;


public class TonResponse
{
    public bool Mintable { get; set; }
    public string TotalSupply { get; set; }
    public JettonAdmin Admin { get; set; }
    public JettonMetadata Metadata { get; set; }
    public string Preview { get; set; }
    public string Verification { get; set; }
    public int HoldersCount { get; set; }
    public int Score { get; set; }
    public string Error { get; set; }
}
public class JettonAdmin
{
    public string Address { get; set; }
    public string Name { get; set; }
    public bool IsScam { get; set; }
    public string Icon { get; set; }
    public bool IsWallet { get; set; }
}
public class JettonMetadata
{
    public string Address { get; set; }
    public string Name { get; set; }
    public string Symbol { get; set; }
    public string Decimals { get; set; }
    public string Image { get; set; }
    public string Description { get; set; }
    public List<List<string>> Social { get; set; }
    public List<List<string>> Websites { get; set; }
    public List<List<string>> Catalogs { get; set; }
    public string CustomPayloadApiUri { get; set; }
}

