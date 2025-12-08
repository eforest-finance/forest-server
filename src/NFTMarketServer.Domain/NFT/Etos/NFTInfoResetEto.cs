using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;

[EventName("NFTInfoResetEto")]
public class NFTInfoResetEto
{
    public string NFTInfoId { get; set; }
    public string ChainId { get; set; }
    public NFTType NFTType { get; set; }
}

public enum NFTType
{
    Seed,
    NFT
}