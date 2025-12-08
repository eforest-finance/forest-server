using System.Collections.Generic;
using NFTMarketServer.Seed.Index;

namespace NFTMarketServer.NFT.Index;

public class IndexerSeedSymbolSync : IndexerCommonResult<IndexerSeedSymbolSync>
{
    public long TotalRecordCount { get; set; }
    
    public List<SeedSymbolIndex> DataList { get; set; }
}