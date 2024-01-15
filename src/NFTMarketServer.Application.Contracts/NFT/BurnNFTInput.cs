using System;

namespace NFTMarketServer.NFT
{
    public class BurnNFTInput:InputBase
    {
        public string Burner { get; set; }
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public long Amount { get; set; }
        public DateTime BurnTime { get; set; }
    }
}