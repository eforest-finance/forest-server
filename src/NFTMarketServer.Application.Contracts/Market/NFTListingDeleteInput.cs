using System;

namespace NFTMarketServer.Market
{
    public class NFTListingDeleteInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string Owner { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime PublicTime { get; set; }
        public DateTime EndTime { get; set; }
        public NFTListType ListType { get; set; }
    }
}