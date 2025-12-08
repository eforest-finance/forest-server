using System.Collections.Generic;

namespace NFTMarketServer.NFT
{
    public class UpdateNFTInfoInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public List<MetadataDto> Metadata { get; set; }
        public string Alias { get; set; }
        public string Uri { get; set; }
    }
}