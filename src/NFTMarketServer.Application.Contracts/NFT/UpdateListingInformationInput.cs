using System;

namespace NFTMarketServer.NFT
{
    public class UpdateListingInformationInput:InputBase
    {
        public Guid NFTInfoId { get; set; }
        public Guid ListingId { get; set; }
        public string ListingAddress { get; set; }
        public Guid ListingTokenId { get; set; } 
        public decimal ListingPrice { get; set; }
        public long ListingQuantity { get; set; }
        public DateTime ListingEndTime { get; set; }
        public bool IsDelist { get; set; }
    }
}