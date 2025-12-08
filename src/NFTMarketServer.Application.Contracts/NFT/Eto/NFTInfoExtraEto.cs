using NFTMarketServer.Entities;
using Volo.Abp.EventBus;

namespace NFTMarketServer.NFT.Eto;
[EventName("NFTInfoExtraEto")]
    public class NFTInfoExtraEto : MultiChainEntity<string>
    {
        public override string Id    { get; set; }
        public string ChainId { get; set; }
        public string NFTSymbol { get; set; }
        public string PreviewImage { get; set; }
        public string File { get; set; }
        public string FileExtension { get; set; }
        public string Description { get; set; }
        public string TransactionId { get; set; }
        public string ExternalLink { get; set; } 
        public string CoverImageUrl { get; set; }
     
    }

