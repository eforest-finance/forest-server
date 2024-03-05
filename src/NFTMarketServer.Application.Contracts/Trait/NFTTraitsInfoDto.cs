#nullable enable
using System.Collections.Generic;
using NFTMarketServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Trait;

public class NFTTraitsInfoDto : EntityDto<string>
{
    public int Generation { get; set; }
    public List<NFTTraitInfoDto> TraitInfos { get; set; }
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
}