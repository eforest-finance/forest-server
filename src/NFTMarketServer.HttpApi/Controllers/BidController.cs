using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Bid")]
[Route("api/app/bid")]
public class BidController : AbpController
{
    private readonly IBidAppService _bidAppService;
    
    public BidController(IBidAppService bidAppService)
    {
        _bidAppService = bidAppService;
    }
    
    [HttpGet]
    [Route("bid-infos")]
    public async Task<PagedResultDto<BidInfoDto>> GetBidInfosAsync(GetSymbolBidInfoListRequestDto input)
    {
        return await _bidAppService.GetSymbolBidInfoListAsync(input);
    }

    [HttpGet]
    [Route("auction-info")]
    public async Task<AuctionInfoDto> GetAuctionInfosAsync(GetSymbolAuctionInfoRequestDto input)
    {
        return await _bidAppService.GetSymbolAuctionInfoAsync(input.SeedSymbol);
    }
}