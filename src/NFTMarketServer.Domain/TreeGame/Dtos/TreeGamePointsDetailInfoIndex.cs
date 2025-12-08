using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;
using NFTMarketServer.TreeGame;
using Orleans;
using TimeUnit = NFTMarketServer.TreeGame.TimeUnit;

namespace NFTMarketServer.Users.Index;
[GenerateSerializer]

public class TreeGamePointsDetailInfoDto
{
    [Id(0)]
    public string Id { get; set; }

    //userAccount Address
    [Id(1)]
    public string Address { get; set; }

    [Id(2)]
    public PointsDetailType Type { get; set; }

    [Id(3)]
    public decimal Amount { get; set; }

    //13 timestamp
    [Id(4)]
    public long UpdateTime { get; set; }

    [Id(5)]
    public long RemainingTime { get; set; }

    [Id(6)]
    public TimeUnit TimeUnit{ get; set; }

    [Id(7)]
    public int ClaimLimit{ get; set; }
}