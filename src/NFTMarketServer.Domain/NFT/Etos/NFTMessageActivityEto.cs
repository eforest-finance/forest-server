using NFTMarketServer.NFT.Dtos;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;

[EventName("NFTMessageActivityEto")]
public class NFTMessageActivityEto
{
    public NFTMessageActivityDto NFTMessageActivityDto { get; set; }
}