using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;
using NFTMarketServer.TreeGame;
using TimeUnit = NFTMarketServer.TreeGame.TimeUnit;

namespace NFTMarketServer.Users.Index;

public class TreeGamePointsDetailInfoIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    //userAccount Address
    [Keyword] public string Address { get; set; }
    
    public PointsDetailType Type { get; set; }
    
    public long Amount { get; set; }
    
    //13 timestamp
    public long UpdateTime { get; set; }
    
    public long RemainingTime { get; set; }
    
    public TimeUnit TimeUnit{ get; set; }
    public long ClaimLimit{ get; set; }

}