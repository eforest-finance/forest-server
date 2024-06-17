using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Message;

public interface IMessageService
{
    Task<PagedResultDto<MessageInfoDto>> GetMessageListAsync(QueryMessageListInput input);

}