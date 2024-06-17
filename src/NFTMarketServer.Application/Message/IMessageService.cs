using System.Threading.Tasks;

namespace NFTMarketServer.Message;

public interface IMessageService
{
    Task<MessageInfoDto> GetMessageListAsync();

}