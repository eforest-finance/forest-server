using NFTMarketServer.Entities;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Eto;
[EventName("NFTListingChangeEto")]
    public class NFTListingChangeEto
    {
        public string Symbol { get; set; }
    }

