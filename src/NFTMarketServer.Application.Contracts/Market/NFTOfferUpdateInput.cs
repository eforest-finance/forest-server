using System;

namespace NFTMarketServer.Market
{
    public class NFTOfferUpdateInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public decimal Price { get; set; }
        public Guid PurchaseTokenId { get; set; }
        public long Quantity { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}