using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class GetAIArtsInput : PagedAndSortedResultRequestDto
{
    public string Address { get; set; }
    public int Status { get; set; }

}