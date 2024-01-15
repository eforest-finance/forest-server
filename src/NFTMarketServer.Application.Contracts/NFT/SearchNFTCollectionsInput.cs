using Nest;

namespace NFTMarketServer.NFT
{
    public class SearchNFTCollectionsInput : PagedAndMaxCountResultRequestDto
    {
        public string TokenName { get; set; }
        
        public string Sort { get; set; }

        public SortOrder SortType { get; set; } = SortOrder.Descending;
    }
}