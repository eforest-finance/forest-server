#nullable enable
using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Trait;

public class CollectionGenerationInfoDto : EntityDto<string>
{
    public int TotalCount { get; set; }
    public List<GenerationInfoDto> Items { get; set; }
}

public class GenerationInfoDto
{
    public int Generation { get; set; }
    public long GenerationItemsCount { get; set; }
}