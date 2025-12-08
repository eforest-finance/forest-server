#nullable enable
using System;
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Trait;

public class CollectionRarityInfoDto : EntityDto<string>
{
    public int TotalCount { get; set; }
    public List<RarityInfoDto> Items { get; set; }
}

public class RarityInfoDto
{
    public string Rarity { get; set; }
    public long ItemsCount { get; set; }
}

public class RarityComparer : IComparer<RarityInfoDto>  
{  
    private static readonly string[] Order = { "Diamond", "Emerald", "Platinum", "Gold", "Silver", "Bronze" };  
  
    public int Compare(RarityInfoDto x, RarityInfoDto y)  
    {  
        var indexX = Array.IndexOf(Order, x.Rarity);  
        var indexY = Array.IndexOf(Order, y.Rarity);
        if (indexX == -1) return 1;  
        if (indexY == -1) return -1;  
        return indexX.CompareTo(indexY);  
    }  
}