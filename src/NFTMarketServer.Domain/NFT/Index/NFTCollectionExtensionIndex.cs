using System;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class NFTCollectionExtensionIndex : NFTMarketEntity<string>, IIndexBuild
{
    [Keyword] public override string Id { get; set; }
    [Keyword] public string ChainId { get; set; }
    [Keyword] public string NFTSymbol { get; set; }

    [Keyword] public string LogoImage { get; set; }

    [Keyword] public string FeaturedImage { get; set; }

    [Keyword] public string Description { get; set; }

    [Keyword] public string TransactionId { get; set; }
    [Keyword] public string ExternalLink { get; set; } 
    
    public long ItemTotal { get; set; }
    
    public long OwnerTotal { get; set; }

    /**
     * default-1
     */
    public decimal FloorPrice { get; set; } = -1;
    
    [Keyword] public string FloorPriceSymbol { get; set; }
    
    [Keyword] public string TokenName { get; set; }
    
    public DateTime CreateTime { get; set; }
    
    public decimal CurrentDayVolumeTotal { get; set; } = 0;
    public decimal PreviousDayVolumeTotal { get; set; } = 0;
    public decimal CurrentDayVolumeTotalChange { get; set; } = 0;
    
    public decimal CurrentWeekVolumeTotal { get; set; } = 0;
    public decimal PreviousWeekVolumeTotal { get; set; } = 0;
    public decimal CurrentWeekVolumeTotalChange { get; set; } = 0;
    
    public long CurrentDaySalesTotal { get; set; } = 0;
    public long PreviousDaySalesTotal { get; set; } = 0;
    
    public long CurrentWeekSalesTotal { get; set; } = 0;
    public long PreviousWeekSalesTotal { get; set; } = 0;
    
    public decimal PreviousDayFloorPrice { get; set; } = -1;
    public decimal CurrentDayFloorChange { get; set; } = 0;
    
    public decimal PreviousWeekFloorPrice { get; set; } = -1;
    public decimal CurrentWeekFloorChange { get; set; } = 0;

    public long SupplyTotal { get; set; } = 0;
    
}