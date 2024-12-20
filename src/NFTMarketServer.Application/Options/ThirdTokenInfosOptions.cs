using System.Collections.Generic;

namespace NFTMarketServer.Options;

public class ThirdTokenInfosOptions
{
    public string Abi { get; set; }
    public List<ThirdTokenInfo> Chains { get; set; }
}

public class ThirdTokenInfo
{
    public string ChainName { get; set; }
    public string ContractAddress { get; set; }
    public string Url { get; set; }
}