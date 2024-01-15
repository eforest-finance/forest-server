using System.Linq.Dynamic.Core;
using System.Threading.Tasks;
using NFTMarketServer.OwnerShip.Dto;
using NFTMarketServer.Symbol;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace NFTMarketServer.OwnerShip;

public interface IVerifyAppService : IApplicationService
{
    /**
     * auto verify
     */
    Task<AutoVerifyResultDto> OwnerShipVerifyAsync(AutoVerifyInput input);
    /**
     * commit Manual verify
     */
    Task<ResultDto> CommitManualVerifyAsync(CommitManualVerifyInput input);

    Task AddOrUpdateOwnerShipVerifyOrderAsync(OwnerShipVerifyOrder ownerShipVerifyOrder);
    
    Task<ResultDto> RetryVerifyAsync(ApprovalInput input);
    
    Task<ResultDto> SendProposalAsync(ApprovalInput input);
    Task<ResultDto> ApproveVerifyAsync(ApprovalInput input);
    Task<ResultDto> RejectVerifyAsync(ApprovalInput input);

    Task<PagedResultDto<OwnerShipVerifyOrderSummaryDto>> GetOrderSummaryListAsync(GetOrderSummaryListInput input);
    Task<OwnerShipVerifyOrderDetailDto> GetVerifyOrderDetailAsync(GetVerifyOrderDetailInput input);
}