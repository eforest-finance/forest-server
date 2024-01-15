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
        public long Quantity { get; set; }
        public long ExpireTime { get; set; }
        public NFTImmutableInfoDto NftInfo { get; set; }
        public TokenDto PurchaseToken { get; set; }
    }
}