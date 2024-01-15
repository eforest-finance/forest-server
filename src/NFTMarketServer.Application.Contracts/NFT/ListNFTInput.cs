using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT
{
    public class ListNFTInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string Owner { get; set; }
        public long Quantity { get; set; }
        public long Price { get; set; }
        public string PurchaseSymbol { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime PublicTime { get; set; }
        public DateTime EndTime { get; set; }
        public bool IsMergedToPreviousListedInfo { get; set; }
        public List<NFTListingWhiteListInput> NFTListingWhiteLists { get; set; }
    }

    public class NFTListingWhiteListInput
    {
        public string Address { get; set; }
        public long Price { get; set; }
        public string PurchaseSymbol { get; set; }
    }
}