using NFTMarketServer.ThirdToken.Index;

namespace NFTMarketServer.Grains.Grain.ThirdToken;

[GenerateSerializer]
public class TokenRelationGrainDto
{
    [Id(0)] public string Id { get; set; }
    [Id(1)] public string Address { get; set; }
    [Id(2)] public string AelfChain { get; set; }
    [Id(3)] public string AelfToken { get; set; }
    [Id(4)] public string ThirdChain { get; set; }
    [Id(5)] public string ThirdToken { get; set; }
    [Id(6)] public long CreateTime { get; set; }
    [Id(7)] public long UpdateTime { get; set; }
    [Id(8)] public RelationStatus RelationStatus { get; set; }
    [Id(9)] public string ThirdTokenSymbol { get; set; }
}