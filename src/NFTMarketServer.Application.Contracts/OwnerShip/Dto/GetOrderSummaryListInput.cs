using NFTMarketServer.Symbol;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.OwnerShip.Dto;

public class GetOrderSummaryListInput : PagedAndSortedResultRequestDto
{
    public ApprovalStatus ApprovalStatus { get; set; }
}