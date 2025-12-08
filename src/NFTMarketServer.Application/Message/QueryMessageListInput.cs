using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Message;

public class QueryMessageListInput : PagedAndSortedResultRequestDto
{
    public int Status{ get; set; }
}