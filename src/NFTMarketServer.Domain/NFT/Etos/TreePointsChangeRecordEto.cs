using NFTMarketServer.NFT.Index;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;
[EventName("TreePointsChangeRecordEto")]

public class TreePointsChangeRecordEto
{
    public TreePointsChangeRecordItem TreePointsChangeRecordItem { get; set; }
}