using NFTMarketServer.NFT.Dtos;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;

[EventName("NFTActivitySyncEto")]
public class NFTActivitySyncEto
{
    public NFTActivitySyncDto NFTActivitySyncDto { get; set; }
}