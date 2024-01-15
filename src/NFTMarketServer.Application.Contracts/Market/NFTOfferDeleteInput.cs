using System;

namespace NFTMarketServer.Market
{
    public class NFTOfferDeleteInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string From { get; set; }
        public string To { get; set; }
        public DateTime ExpireTime { get; set; }
    }
}