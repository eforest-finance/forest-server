using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.NFT
{
    public class UpdateNFTInfoExtensionInput
    {
        [MaxLength(1000)] 
        public string Description { get; set; }
    }
}