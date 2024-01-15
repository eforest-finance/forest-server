using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Tokens;

namespace NFTMarketServer.Seed.Index;

public class UniqueSeedPriceIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    [Keyword] public string TokenType { get; set; }

    public int SymbolLength { get; set; }

    public TokenPriceDto TokenPrice { get; set; }
    
    
    public long BlockHeight { get; set; }
}