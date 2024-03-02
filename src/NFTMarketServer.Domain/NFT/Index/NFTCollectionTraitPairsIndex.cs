using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class NFTCollectionTraitPairsIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string NFTCollectionSymbol { get; set; }
    [Keyword] public string TraitKey { get; set; }
    [Keyword] public string TraitValue { get; set; }
    public long ItemCount { get; set; }
    [Keyword] public string FloorPriceNFTSymbol { get; set; }
    [Keyword] public string FloorPriceSymbol { get; set; }
    
    public TokenInfoIndex FloorPriceToken { get; set; }
}