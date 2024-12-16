using Nethereum.ABI.FunctionEncoding.Attributes;

namespace NFTMarketServer.ThirdToken.Provider;

public class ThirdTokenInfoContractDto
{
    [Parameter("address", "tokenAddress", 1)]
    public string TokenAddress { get; set; }

    [Parameter("string", "name", 2)] public string Name { get; set; }

    [Parameter("string", "symbol", 3)] public string Symbol { get; set; }

    [Parameter("address", "owner", 4)] public string Owner { get; set; }
}