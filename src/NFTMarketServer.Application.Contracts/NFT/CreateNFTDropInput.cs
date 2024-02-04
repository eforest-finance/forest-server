using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.NFT
{
    public class CreateNFTDropInput
    {
        [Required]public string DropId { get; set; }
        [Required][MaxLength(100)]public string DropName { get; set; }
        [MaxLength(300)] public string Introduction { get; set; }
        
        [Required]public string BannerUrl { get; set; }
        [Required]public string LogoUrl { get; set; }
        [Required]public string TransactionId { get; set; }
        public long StartTime { get; set; }
        public long ExpireTime { get; set; }
        public List<SocialMedia> SocialMedia { get; set; }
    }

    public class SocialMedia
    {
        public string Type { get; set; }
        public string Link { get; set; }
    }
}