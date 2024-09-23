using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public class ProfileInfo
    {
        public decimal Balance { get; set; }
        public long Decimal { get; set; }
        public decimal? MinListingPrice { get; set; }
        public decimal? BestOfferPrice { get; set; }
        public string ShowPrice { get; set; } = "--";
        
    }
}