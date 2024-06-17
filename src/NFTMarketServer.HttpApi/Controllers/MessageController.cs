using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Message;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Dto;
using Volo.Abp;

namespace NFTMarketServer.Controllers
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Message")]
    [Route("api/app/message")]
    public class MessageController : NFTMarketServerController
    {
        private readonly IMessageService _messageService;

        public MessageController(IMessageService messageService)
        {
            _messageService = messageService;
        }

        [HttpGet]
        [Route("list")]
        [Authorize]
        public Task<List<MessageInfoDto>> GetMessageListAsync()
        {
            return _messageService.GetMessageListAsync();
        }
        
    }
}