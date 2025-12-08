using System;

namespace NFTMarketServer.Market
{
    public class NFTListingUpdateInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string Owner { get; set; }
        public decimal Price { get; set; }
        public Guid PurchaseTokenId { get; set; }
        public long Quantity { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime PublicTime { get; set; }
        public DateTime EndTime { get; set; }
        public DateTime PreviousStartTime { get; set; }
        public DateTime PreviousPublicTime { get; set; }
        public DateTime PreviousEndTime { get; set; }
        public NFTListType ListType { get; set; }
        public string WhitelistHash { get; set; }
    }
}