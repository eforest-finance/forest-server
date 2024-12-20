using NFTMarketServer.ThirdToken.Index;

namespace NFTMarketServer.Grains.Grain.ThirdToken;

[GenerateSerializer]
public class ThirdTokenGrainDto
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Symbol { get; set; }
    [Id(2)] public string TokenName { get; set; }
    [Id(3)] public string Chain { get; set; }
    [Id(4)] public long TotalSupply { get; set; }
    [Id(5)] public int Decimals { get; set; }
    [Id(6)] public string Owner { get; set; }
    [Id(7)] public long CreateTime { get; set; }
    [Id(8)] public string TokenImage { get; set; }
    [Id(9)] public string ContractAddress { get; set; }
    [Id(10)] public ThirdTokenStatus ThirdTokenStatus { get; set; }
    [Id(11)] public string Address { get; set; }
}