using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Market;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Dto;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("ai")]
[ControllerName("AI")]
[Route("api/app/ai")]
public class AIController : AbpController
{
    private readonly IBookAppService _bookAppService;

    public AIController(IBookAppService bookAppService)
    {
        _bookAppService = bookAppService;
    }

    [HttpPost]
    [Route("create")]
    public async Task<BookDto> CreateBookAsync( CreateBookCommand command)
    {
        return await _bookAppService.CreateBookAsync(command);
    }
    
    [HttpGet]
    [Route("get")]
    public async Task<BookDto> GetBookAsync(int id)
    {
        
        return await _bookAppService.GetBookAsync(new GetBookQuery(){Id = id});
    }
    
    
}