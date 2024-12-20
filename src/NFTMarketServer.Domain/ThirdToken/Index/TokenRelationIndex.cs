using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.ThirdToken.Index;

public class TokenRelationIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string AelfChain { get; set; }
    [Keyword] public string AelfToken { get; set; }
    [Keyword] public string ThirdChain { get; set; }
    [Keyword] public string ThirdToken { get; set; }
    [Keyword] public string ThirdTokenSymbol { get; set; }
    public long CreateTime { get; set; }
    public long UpdateTime { get; set; }
    public RelationStatus RelationStatus { get; set; }
}

public enum RelationStatus
{
    Binding = 0,
    Bound = 1,
    Unbound = 2,
}