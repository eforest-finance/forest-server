#nullable enable
using System.Collections.Generic;
using NFTMarketServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Trait;

public class NFTTraitsInfoDto : EntityDto<string>
{
    public int Generation { get; set; } = -1;
    public List<NFTTraitInfoDto> TraitInfos { get; set; }
    
    public int Rank { get; set; }
    public string Level { get; set; }
    public string Grade { get; set; }
    public string Star{ get; set; }
    public string Rarity { get; set; }
    public string Describe { get; set; }
}

public class NFTTraitInfoDto
{
    public string Id { get; set; }
    public string Key { get; set; }
    public string Value { get; set; }
    public long ItemsCount { get; set; } = -1;
    public long AllItemsCount { get; set; } = -1;
    public decimal? ItemFloorPrice { get; set; } = -1;
    public TokenDto? ItemFloorPriceToken { get; set; }
    
    public decimal? LatestDealPrice { get; set; } = -1;

}