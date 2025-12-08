using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Users;

public class QueryUserBalanceListInput : PagedAndSortedResultRequestDto
{
    public int Status{ get; set; }
}