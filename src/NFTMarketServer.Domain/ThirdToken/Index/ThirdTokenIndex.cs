using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.ThirdToken.Index;

public class ThirdTokenIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Symbol { get; set; }
    [Keyword] public string TokenName { get; set; }
    [Keyword] public string Chain { get; set; }
    public long TotalSupply { get; set; }
    public int Decimals { get; set; }
    [Keyword] public string Owner { get; set; }
    public long CreateTime { get; set; }
    [Keyword] public string TokenImage { get; set; }
    [Keyword] public string ContractAddress { get; set; }
    public ThirdTokenStatus ThirdTokenStatus { get; set; }
}

public enum ThirdTokenStatus
{
    Creating = 0,
    Created = 1,
}