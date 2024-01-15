using Volo.Abp.EventBus;

namespace NFTMarketServer.Bid.Eto;

[EventName("AuctionClaimTransactionInfoEto")]
public class AuctionClaimTransactionInfoEto
{
    public string AuctionId { get; set; }
}