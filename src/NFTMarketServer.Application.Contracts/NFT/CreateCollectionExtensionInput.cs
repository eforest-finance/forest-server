namespace NFTMarketServer.NFT
{
    public class CreateCollectionExtensionInput
    {
        public string ChainId { get; set; }
        public string Symbol { get; set; }
        public string TransactionId { get; set; }
        public string Description { get; set; } 
        public string ExternalLink { get; set; } 
        public string LogoImage { get; set; }
        public string FeaturedImage { get; set; }
        public string TokenName { get; set; }
    }
}