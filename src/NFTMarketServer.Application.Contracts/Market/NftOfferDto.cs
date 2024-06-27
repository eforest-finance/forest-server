using NFTMarketServer.NFT;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public class NFTOfferDto : EntityDto<string>
    {
        public string ChainId { get; set; }
        public string FromAddress { get; set; }
        public string ToAddress { get; set; }
        public AccountDto From { get; set; }
        public AccountDto To { get; set; }
        public decimal Price { get; set; }
        public decimal FloorPrice { get; set; } = -1;
        public string FloorPriceSymbol { get; set; }
        public long Quantity { get; set; }
        public long ExpireTime { get; set; }
        public TokenDto PurchaseToken { get; set; }
    }

    public class CollectedCollectionOffersDto : NFTOfferDto
    {
        public string CollectionName { get; set; }
        public string NFTName { get; set; }
        
        public string PreviewImage { get; set; }
        
        public int Decimals { get; set; }
        
        public string NFTSymbol { get; set; }
        
        public string NFTInfoId { get; set; }
    }
}