using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class NFTCollectionTraitKeyIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string NFTCollectionSymbol { get; set; }
    [Keyword] public string TraitKey { get; set; }
    public long ItemCount { get; set; }
}