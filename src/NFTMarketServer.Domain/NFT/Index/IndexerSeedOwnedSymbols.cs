using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index;

public class IndexerSeedOwnedSymbols
{
    public long TotalRecordCount { get; set; }
    public List<IndexerSeedOwnedSymbol> IndexerSeedOwnedSymbolList { get; set; }
}

public class IndexerSeedOwnedSymbol : IndexerCommonResult<IndexerSeedOwnedSymbol>
{
    public string Id { get; set; }

    public string Symbol { get; set; }
    public string SeedSymbol { get; set; }
    
    public string Issuer { get; set; }

    public bool IsBurnable { get; set; }
    
    public DateTime CreateTime { get; set; }

    public long SeedExpTimeSecond { get; set; }

    public DateTime SeedExpTime { get; set; }
    
    public string ChainId { get; set; }
}
