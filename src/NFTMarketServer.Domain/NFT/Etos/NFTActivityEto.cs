using NFTMarketServer.NFT.Dtos;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;

[EventName("NFTActivityEto")]
public class NFTActivityEto
{
    public NFTMessageActivityDto NFTMessageActivityDto { get; set; }
}