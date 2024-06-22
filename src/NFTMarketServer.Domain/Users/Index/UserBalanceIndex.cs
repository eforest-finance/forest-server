using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Users.Index;

public class UserBalanceIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    //userAccount Address
    [Keyword] public string Address { get; set; }

    public long Amount { get; set; }

    [Keyword] public string NFTInfoId { get; set; }

    [Keyword] public string Symbol { get; set; }

    public DateTime ChangeTime { get; set; }

    public decimal ListingPrice { get; set; }
    public DateTime? ListingTime { get; set; }
}