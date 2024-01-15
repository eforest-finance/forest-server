using System.Collections.Generic;

namespace NFTMarketServer.Market
{
    public class UpdateGlobalPurchaseTokensInput:InputBase
    {
        public List<string> PurchaseSymbols { get; set; }
    }
}