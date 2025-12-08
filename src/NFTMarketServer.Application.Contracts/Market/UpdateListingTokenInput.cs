using System;

namespace NFTMarketServer.Market
{
    public class UpdateListingTokenInput:InputBase
    {
        public Guid NFTInfoId { get; set; }
        public Guid? ListingTokenId { get; set; }
    }
}