using System;

namespace NFTMarketServer.NFT
{
    public class GetNFTInfoMinterWhiteListInput
    { 
        public string Address { get; set; }
        public Guid? CollectionId { get; set; }
    }
}