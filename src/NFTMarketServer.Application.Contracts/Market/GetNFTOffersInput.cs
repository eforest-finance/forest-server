using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public class GetNFTOffersInput: PagedAndSortedResultRequestDto
    {
        [Required]
        public string ChainId { get; set; }
        public string NFTInfoId { get; set; }
        
        public string ExcludeAddress{ get; set; }
    }
}