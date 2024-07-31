using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Dto;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("app")]
[ControllerName("Seed")]
[Route("api/app/seed")]
public class SeedController : AbpController
{
    private readonly ISeedAppService _seedAppService;

    public SeedController(ISeedAppService seedAppService)
    {
        _seedAppService = seedAppService;
    }

    [HttpGet]
    [Route("search-symbol-info")]
    public async Task<SeedDto> SearchSymbolInfo(SearchSeedInput searchSeedInput)
    {
        return await _seedAppService.SearchSeedInfoAsync(searchSeedInput);
    }

    [HttpGet]
    [Route("symbol-info")]
    public async Task<SeedDto> GetSymbolInfo(QuerySeedInput querySeedInput)
    {
        return await _seedAppService.GetSeedInfoAsync(querySeedInput);
    }

    [HttpGet]
    [Route("my-seed")]
    public async Task<PagedResultDto<SeedDto>> MySeed(MySeedInput querySeedInput)
    {
        return await _seedAppService.MySeedAsync(querySeedInput);
    }


    [HttpGet]
    [Route("special-seeds")]
    public Task<PagedResultDto<SpecialSeedDto>> GetSpecialSymbolListAsync(QuerySpecialListInput input)
    {
        return _seedAppService.GetSpecialSymbolListAsync(input);
    }
    
    [HttpGet]
    [Route("bidding-seeds")]
    public Task<PagedResultDto<BiddingSeedDto>> GetBiddingSeedsAsync(GetBiddingSeedsInput input)
    {
        return _seedAppService.GetBiddingSeedsAsync(input);
    }

    [HttpGet]
    [Route("bid-price")]
    public Task<BidPricePayInfoDto> GetSymbolBidPriceAsync(QueryBidPricePayInfoInput payInput)
    {
        return _seedAppService.GetSymbolBidPriceAsync(payInput);
    }

    [AllowAnonymous]
    [HttpPost]
    [Route("seed")]
    [Authorize]
    public async Task<CreateSeedResultDto> CreateSeedAsync(CreateSeedDto input)
    {
        return await _seedAppService.CreateSeedAsync(input);
    }
    
    [HttpGet]
    [Route("transaction-fee")]
    public async Task<TransactionFeeDto> GetTransactionFeeAsync(QueryTransactionFeeInput input)
    {
        return await _seedAppService.GetTransactionFeeAsync(input.Symbol);
    }
    
    [HttpPut]
    [Route("ranking-weight")]
    [Authorize]
    public virtual Task  UpdateSeedWeightAsync(List<SeedRankingWeightDto> inputList)
    {
        if (inputList.IsNullOrEmpty())
        {
            throw new UserFriendlyException("Invalid input");
        }
        return _seedAppService.UpdateSeedRankingWeightAsync(inputList);
    }
    
    [HttpGet]
    [Route("ranking-weight")]
    public async Task<PagedResultDto<SeedRankingWeightDto>> GetSeedRankingWeightInfosAsync()
    {
        return await _seedAppService.GetSeedRankingWeightInfosAsync();
    }
}