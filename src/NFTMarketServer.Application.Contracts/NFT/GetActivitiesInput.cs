using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class GetActivitiesInput : PagedAndSortedResultRequestDto
{
    
    public string NFTInfoId { get; set; }
    public List<int> Types { get; set; }
    public long TimestampMin { get; set; }
    public long TimestampMax { get; set; }
    
    public string FilterSymbol { get; set; }

}