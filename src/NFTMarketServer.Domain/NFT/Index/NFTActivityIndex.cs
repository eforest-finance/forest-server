using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class NFTActivityIndex :  NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    
    [Keyword] public string NftInfoId { get; set; }
    
    public NFTActivityType Type { get; set; }
    
    [Keyword] public string From { get; set; }
    
    [Keyword] public string To { get; set; }
    
    public long Amount { get; set; }
    
    public decimal Price { get; set; }
    
    [Keyword] public string TransactionHash { get; set; }
    
    public DateTime Timestamp { get; set; }
}