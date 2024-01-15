using NFTMarketServer.Order;

namespace NFTMarketServer.Grains.Grain.Order;

public class NFTOrderGrainDto : NFTOrder
{
    public Dictionary<string, string> ExternalInfo { get; set; }
}