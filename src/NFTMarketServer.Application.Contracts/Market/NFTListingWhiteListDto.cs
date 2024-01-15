using NFTMarketServer.Tokens;

namespace NFTMarketServer.Market
{
    public class NFTListingWhiteListDto
    {
        public string Address { get; set; }
        public decimal Price { get; set; }
        public TokenDto PurchaseToken { get; set; }
    }
}