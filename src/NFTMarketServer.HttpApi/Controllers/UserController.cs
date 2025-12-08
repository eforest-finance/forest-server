using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Dto;
using Volo.Abp;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("User")]
    [Route("api/app/users")]
    public class UserController : NFTMarketServerController
    {
        private readonly IUserAppService _userAppService;

        public UserController(IUserAppService userAppService)
        {
            _userAppService = userAppService;
        }

        [HttpGet]
        [Route("by-address")]
        public Task<UserDto> GetByUserNameAsync(string address)
        {
            return _userAppService.GetByUserAddressAsync(address);
        }

        [HttpGet]
        [Authorize]
        [Route("check-name")]
        public Task<bool> CheckNameAsync(string name)
        {
            return _userAppService.CheckNameAsync(name);
        }

        [HttpPut]
        [Authorize]
        public Task UpdateUserAsync(UserUpdateDto input)
        {
            return _userAppService.UserUpdateAsync(input);
        }
        
        [HttpGet]
        [Route("count")]
        public Task<long> CountAsync(DateTime beginTime, DateTime endTime)
        {
            return _userAppService.GetUserCountAsync(beginTime, endTime);
        }
    }
}