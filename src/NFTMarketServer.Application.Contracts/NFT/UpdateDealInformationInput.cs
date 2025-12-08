using System;

namespace NFTMarketServer.NFT
{
    public class UpdateDealInformationInput:InputBase
    {
        public Guid NFTInfoId { get; set; }
        public decimal DealPrice { get; set; }
        public DateTime DealTime { get; set; }
    }
}