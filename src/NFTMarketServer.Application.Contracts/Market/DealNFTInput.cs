using System;

namespace NFTMarketServer.Market
{
    public class DealNFTInput:InputBase
    {
        public string NFTFrom { get; set; }
        public string NFTTo { get; set; }
        public string NFTSymbol { get; set; }
        public long NFTTokenId { get; set; }
        public long NFTQuantity { get; set; }
        public string  PurchaseSymbol { get; set; }
        public long  PurchaseAmount { get; set; }
        public long  PurchaseTokenId { get; set; }
        public DateTime DealTime { get; set; }
    }
}