using NFTMarketServer.NFT.Dtos;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;

[EventName("NFTActivitySyncEto")]
public class NFTActivitySyncEto
{
    public NFTActivitySyncDto NFTActivitySyncDto { get; set; }
}

[EventName("NFTActivityTransferSyncEto")]
public class NFTActivityTransferSyncEto
{
    public NFTActivitySyncDto NFTActivitySyncDto { get; set; }
}