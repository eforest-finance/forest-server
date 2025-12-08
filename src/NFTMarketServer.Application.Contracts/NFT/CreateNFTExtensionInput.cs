using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.NFT
{
    public class CreateNFTExtensionInput
    {
        public string ChainId { get; set; }
        public string Symbol { get; set; }
        public string TransactionId { get; set; }
        public string PreviewImage { get; set; }
        public string File { get; set; }
        [MaxLength(1000)]
        public string Description { get; set; } 
        public string ExternalLink { get; set; } 
        public string CoverImageUrl { get; set; }

    }
}