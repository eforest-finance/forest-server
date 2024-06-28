using System.Collections.Generic;
using NFTMarketServer.NFT;

namespace NFTMarketServer.Users
{
    public class QueryUserBalanceIndexInput : PagedAndMaxCountResultRequestDto
    {
        public string Address { get; set; }
        public string KeyWord { get; set; }
        public QueryType  QueryType{ get; set; }
        
        public List<string> CollectionIdList{ get; set; }
    }


}