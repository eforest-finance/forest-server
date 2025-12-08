using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Eto;
[EventName("MessageChangeEto")]
    public class MessageChangeEto
    {
        public string Address { get; set; }
    }

