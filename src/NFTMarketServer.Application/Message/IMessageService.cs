using System.Collections.Generic;
using System.Threading.Tasks;

namespace NFTMarketServer.Message;

public interface IMessageService
{
    Task<List<MessageInfoDto>> GetMessageListAsync();

}