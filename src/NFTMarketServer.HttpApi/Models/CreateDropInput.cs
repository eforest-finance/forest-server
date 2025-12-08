using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Models
{
    public class CreateDropInput
    {
        [Required]public string DropId { get; set; }
        [Required][MaxLength(100)]public string DropName { get; set; }
        [MaxLength(300)] public string Introduction { get; set; }
        
        public string BannerUrl { get; set; }
        public string LogoUrl { get; set; }
        public string TransactionId { get; set; }
        public List<SocialMedia> SocialMedia { get; set; }
    }

    public class SocialMedia
    {
        public string Type { get; set; }
        public string Link { get; set; }
    }
}