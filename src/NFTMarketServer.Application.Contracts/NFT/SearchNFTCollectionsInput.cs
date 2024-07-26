using Nest;

namespace NFTMarketServer.NFT
{
    public class SearchNFTCollectionsInput : PagedAndMaxCountResultRequestDto
    {
        public string TokenName { get; set; }
        
        public string Sort { get; set; }

        public DateRangeType DateRangeType { get; set; } = DateRangeType.byday;

        public SortOrder SortType { get; set; } = SortOrder.Descending;
    }

    public enum DateRangeType
    {
        byday,
        byweek,
        bymonth,
        byall
    }
}