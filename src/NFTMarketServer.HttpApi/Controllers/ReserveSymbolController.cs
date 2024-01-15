using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.OwnerShip;
using NFTMarketServer.OwnerShip.Dto;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

// [RemoteService]
// [Area("app")]
// [ControllerName("ReserveSymbol")]
// [Route("api/app/reserve-symbol")]
public class ReserveSymbolController : AbpController
{
    private readonly IVerifyAppService _verifyAppService;

    public ReserveSymbolController(IVerifyAppService verifyAppService)
    {
        _verifyAppService = verifyAppService;
    }

    [HttpPost]
    [Route("ownership-verify")]
    public virtual Task<AutoVerifyResultDto> OwnerShipVerify(AutoVerifyInput input)
    {
        return _verifyAppService.OwnerShipVerifyAsync(input);
    }
    
    [HttpPost]
    [Route("commit-manual-verify")]
    public virtual Task<ResultDto> CommitManualVerify(CommitManualVerifyInput input)
    {
        return _verifyAppService.CommitManualVerifyAsync(input);
    }
    
    [HttpGet]
    [Route("retry-verify")]
    public virtual Task<ResultDto> RetryVerify(ApprovalInput input)
    {
        return _verifyAppService.RetryVerifyAsync(input);
    }
    
    [HttpGet]
    [Route("send-proposal")]
    public virtual Task<ResultDto> SendProposal(ApprovalInput input)
    {
        return _verifyAppService.SendProposalAsync(input);
    }
    
    [HttpGet]
    [Route("approve-verify")]
    public virtual Task<ResultDto> ApproveVerify(ApprovalInput input)
    {
        return _verifyAppService.ApproveVerifyAsync(input);
    }
    
    [HttpGet]
    [Route("reject-verify")]
    public virtual Task<ResultDto> RejectVerify(ApprovalInput input)
    {
        return _verifyAppService.RejectVerifyAsync(input);
    }
    
    [HttpGet]
    [Route("list-verify-order")]
    public virtual Task<PagedResultDto<OwnerShipVerifyOrderSummaryDto>> ListVerifyOrder(GetOrderSummaryListInput input)
    {
        return _verifyAppService.GetOrderSummaryListAsync(input);
    }
    
    [HttpGet]
    [Route("verify-order-detail")]
    public virtual Task<OwnerShipVerifyOrderDetailDto> VerifyOrderDetail(GetVerifyOrderDetailInput input)
    {
        return _verifyAppService.GetVerifyOrderDetailAsync(input);
    }
}