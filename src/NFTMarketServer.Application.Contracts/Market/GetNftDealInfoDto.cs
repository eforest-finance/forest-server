using Nest;

namespace NFTMarketServer.Market;

public class GetNftDealInfoDto
{
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public string CollectionSymbol { get; set; }

    public int SkipCount { get; set; }

    public int MaxResultCount { get; set; }

    public int SortType { get; set; }

    public string Sort { get; set; }
}