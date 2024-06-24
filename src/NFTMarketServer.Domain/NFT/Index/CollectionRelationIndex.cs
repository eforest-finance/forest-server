using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class CollectionRelationIndex :  NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CollectionId { get; set; }
    [Keyword] public string Address { get; set; }
}