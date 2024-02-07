using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class GetNFTDropListInput : PagedAndSortedResultRequestDto
{
    public NFTDropType Type { get; set; }
}