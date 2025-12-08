using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NFTMarketServer.Message;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

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
        public Task<PagedResultDto<MessageInfoDto>> GetMessageListAsync(QueryMessageListInput input)
        {
            return _messageService.GetMessageListAsync(input);
        }
        
    }
}