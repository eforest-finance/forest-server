using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Tree;

public class TreeActivityIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string OriginId { get; set; }
    [Text(Index = false)] public string ImageUrl { get; set; }
    [Keyword] public string ActivityName { get; set; }
    [Text(Index = false)] public string ActivityDesc { get; set; }
    public string RewardName { get; set; }
    [Text(Index = false)] public string Condition { get; set; }
    public decimal TotalReward { get; set; }
    public decimal LeftReward { get; set; }
    [Text(Index = false)] public string RewardLogo { get; set; }
    public RewardType RewardType { get; set; }
    public TreeActivityStatus TreeActivityStatus { get; set; }
    public bool HideFlag { get; set; }
    public DateTime BeginDateTime { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastModifyTime { get; set; }
}