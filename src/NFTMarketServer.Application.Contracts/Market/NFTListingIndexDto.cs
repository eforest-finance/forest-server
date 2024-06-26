using NFTMarketServer.NFT;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public class NFTListingIndexDto : EntityDto<string>
    {
        public string OwnerAddress { get; set; }
        public AccountDto Owner { get; set; }
        public long Quantity { get; set; }
        public decimal Prices { get; set; }
        public decimal? WhitelistPrices { get; set; }
        public string Symbol { get; set; }
        public string ChainId { get; set; }
        public long StartTime { get; set; }
        public long PublicTime { get; set; }
        public long EndTime { get; set; }
        public string WhitelistId { get; set; }
        public NFTImmutableInfoDto NFTInfo { get; set; }
        public TokenDto PurchaseToken { get; set; }
    }
    
    public class CollectedCollectionListingDto : NFTListingIndexDto
    {
        public string CollectionName { get; set; }
        public string NFTName { get; set; }
        
        public string NFTUrl { get; set; }
    }
}