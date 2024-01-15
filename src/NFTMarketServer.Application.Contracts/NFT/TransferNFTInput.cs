namespace NFTMarketServer.NFT
{
    public class TransferNFTInput:InputBase
    {
        public string From { get; set; }
        public string  To { get; set; }
        public string Symbol { get; set; }
        public long TokenId { get; set; }
        public long Amount { get; set; }
    }
}