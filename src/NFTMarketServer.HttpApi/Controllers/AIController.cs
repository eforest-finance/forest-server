using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Market;
using NFTMarketServer.Market.Publish;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Dto;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Users;

namespace NFTMarketServer.Controllers;

[RemoteService]
[Area("ai")]
[ControllerName("AI")]
[Route("api/app/ai")]
public class AIController : AbpController
{
    private readonly IBookAppService _bookAppService;
    private readonly IUserService _userService;

    public AIController(IBookAppService bookAppService,IUserService userService)
    {
        _bookAppService = bookAppService;
        _userService = userService;
    }

    [HttpPost]
    [Route("create")]
    public async Task<int> CreateBookAsync( CreateBookCommand command)
    {
        return await _bookAppService.CreateBookAsync(command);
    }
    
    [HttpGet]
    [Route("get")]
    public async Task<BookDto> GetBookAsync(int id)
    {
        
        return await _bookAppService.GetBookAsync(new GetBookQuery(){Id = id});
    }
    
    [HttpPost]
    [Route("publish")]
    public async Task<Task> CreateUserAsync()
    {
        return _userService.RegisterUserAsync("john.doe", "john.doe@example.com");
    }
    
}