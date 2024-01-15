using System;

namespace NFTMarketServer.Market
{
    public class UpdateMarketDataInput:InputBase
    {
        public Guid NFTInfoId {get; set; }
        public decimal Price { get; set; }
        public DateTime Timestamp { get; set; }
    }
}