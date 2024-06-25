using NFTMarketServer.NFT;

namespace NFTMarketServer.Users
{
    public class QueryMyHoldNFTCollectionsInput : PagedAndMaxCountResultRequestDto
    {
        public string Address { get; set; }
        public string KeyWord { get; set; }
        public QueryType  QueryType{ get; set; }
    }


}