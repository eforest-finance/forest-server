using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Etos;

[EventName("NFTResetFlagEto")]
public class NFTResetFlagEto
{
    public string FlagDesc { get; set; }
    public int Minutes { get; set; }
}