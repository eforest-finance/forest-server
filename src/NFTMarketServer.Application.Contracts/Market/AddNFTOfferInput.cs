using System;

namespace NFTMarketServer.Market
{
    public class AddNFTOfferInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string From  { get; set; }
        public string  To   { get; set; }
        public long Price { get; set; }
        public string PurchaseSymbol { get; set; }
        public long Quantity { get; set; }
        public DateTime ExpireTime { get; set; }
        public DateTime DueTime  { get; set; }
    }
}