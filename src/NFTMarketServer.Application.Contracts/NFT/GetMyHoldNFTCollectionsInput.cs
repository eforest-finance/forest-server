namespace NFTMarketServer.NFT
{
    public class GetMyHoldNFTCollectionsInput : PagedAndMaxCountResultRequestDto
    {
        public string Address { get; set; }
        public string KeyWord { get; set; }
        public QueryType  QueryType{ get; set; }
    }

    public enum QueryType
    {
        HOLDING,
        HOLDED
    }
}