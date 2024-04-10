using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class HourlyCollectionTradeRecordIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string CollectionId { get; set; }
    public long Ordinal { get; set; }
    public DateTime AttributionTime { get; set; }
    public decimal VolumeTotal { get; set; } = -1;
    public decimal FloorPrice { get; set; } = -1;
    public DateTime CreateTime { get; set; }
    public long SalesTotal { get; set; } = -1;

}