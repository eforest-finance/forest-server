using System.Threading.Tasks;

namespace NFTMarketServer.Message;

public class MessageService: NFTMarketServerAppService, IMessageService
{
    public Task<MessageInfoDto> GetMessageListAsync()
    {
        throw new System.NotImplementedException();
    }
}