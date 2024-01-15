using System.Collections.Generic;

namespace NFTMarketServer.Market
{
    public class UpdateCollectionPurchaseTokensInput : InputBase
    {
        public string Symbol { get; set; }
        public List<string> PurchaseSymbols { get; set; }
    }
}