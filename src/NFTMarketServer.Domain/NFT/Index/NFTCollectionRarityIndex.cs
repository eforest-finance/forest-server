using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class NFTCollectionRarityIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string CollectionSymbol { get; set; }
    [Keyword] public string Rarity { get; set; }
   
    public long ItemCount { get; set; }
}