using Nethereum.ABI.FunctionEncoding.Attributes;

namespace NFTMarketServer.ThirdToken.Strategy;

[FunctionOutput]
public class EvmDto : IFunctionOutputDTO
{
    [Parameter("address", "tokenAddress", 1)]
    public string TokenAddress { get; set; }
    
}