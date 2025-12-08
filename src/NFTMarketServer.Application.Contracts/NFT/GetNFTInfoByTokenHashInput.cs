namespace NFTMarketServer.NFT
{
    public class GetNFTInfoByTokenHashInput
    {
        public int ChainId { get; set; }

        public string TokenHash { get; set; }
        public string Address { get; set; }
    }
}