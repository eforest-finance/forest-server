namespace NFTMarketServer.NFT
{
    public class SearchAIArtsInput : PagedAndMaxCountResultRequestDto
    {
        public string Address { get; set; }
    }
}