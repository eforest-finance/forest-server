using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Users.Index;

public class TreeGameUserInfoIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }

    //userAccount Address
    [Keyword] public string Address { get; set; }
    
    public string NickName { get; set; }
    
    public decimal Points { get; set; }
    
    public int TreeLevel { get; set; }

    //13 timestamp
    public long CreateTime { get; set; }
    
    [Keyword] public string ParentAddress { get; set; }

}