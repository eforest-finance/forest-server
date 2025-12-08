using System;
using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.Market
{
    public class GetNFTInfoMarketDataInput : PagedAndSortedMaxCountResultRequestDto
    {
        [Required]
        public String NFTInfoId { get; set; }
        public long TimestampMin { get; set; }
        public long TimestampMax { get; set; }
    }
}