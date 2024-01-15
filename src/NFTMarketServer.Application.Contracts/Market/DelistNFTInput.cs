namespace NFTMarketServer.Market
{
    public class DelistNFTInput:InputBase
    {
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public string Owner { get; set; }
        public long Quantity { get; set; }
    }
}