using System.Collections.Generic;

namespace NFTMarketServer.NFT
{
    public class SearchAIArtsInput : PagedAndMaxCountResultRequestDto
    {
        public string Address { get; set; }
        public int Status { get; set; }
        
        public List<string> ImageIds { get; set; }
    }
}