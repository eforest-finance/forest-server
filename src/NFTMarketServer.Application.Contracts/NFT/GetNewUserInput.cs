using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class GetNewUserInput : PagedAndSortedResultRequestDto
{
    public long TimestampMin { get; set; }
    public long TimestampMax { get; set; }
}