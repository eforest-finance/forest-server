using NFTMarketServer.Order;

namespace NFTMarketServer.Grains.Grain.Order;
[GenerateSerializer]
public class NFTOrderGrainDto : NFTOrder
{
    [Id(0)]
    public Dictionary<string, string> ExternalInfo { get; set; }
}