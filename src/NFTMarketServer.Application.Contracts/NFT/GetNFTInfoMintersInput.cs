using System.Collections.Generic;

namespace NFTMarketServer.NFT
{
    public class GetNFTInfoMintersInput
    {
        public int ChainId { get; set; }
        public List<int> ChainIds { get; set; } = new();
        public string Minter { get; set; }
    }
}