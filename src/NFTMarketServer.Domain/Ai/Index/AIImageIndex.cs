using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.Ai.Index;

public class AIImageIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string Address { get; set; }
    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string Hash { get; set; }
    [Text(Index = false)] public string S3Url { get; set; }
    [Text(Index = false)] public string IpfsId { get; set; }
    public DateTime Ctime { get; set; }
    public DateTime Utime { get; set; }
    
    public int status { get; set; }
}