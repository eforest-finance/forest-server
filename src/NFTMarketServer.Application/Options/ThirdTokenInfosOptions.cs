using System.Collections.Generic;
using NFTMarketServer.ThirdToken.Index;

namespace NFTMarketServer.Options;

public class ThirdTokenInfosOptions
{
    public string Abi { get; set; }
    public List<ThirdTokenInfo> Chains { get; set; }
    public string AutoVerifyUrl { get; set; }
}

public class ThirdTokenInfo
{
    public string ChainName { get; set; }
    public string ContractAddress { get; set; }
    public string Url { get; set; }
    
    public ThirdTokenType Type { get; set; }
}