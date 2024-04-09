using System;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class UserBalanceBase : NFTMarketEntity<string>
{
    [Keyword] public override string Id { get; set; }

    //userAccount Address
    [Keyword] public string Address { get; set; }

    public long Amount { get; set; }

    public int Decimals { get; set; }

    [Keyword] public string NFTInfoId { get; set; }

    [Keyword] public string Symbol { get; set; }

    [Keyword] public string BlockHash { get; set; }

    public long BlockHeight { get; set; }

    public DateTime ChangeTime { get; set; }
}