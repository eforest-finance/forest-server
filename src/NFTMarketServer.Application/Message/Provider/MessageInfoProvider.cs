using System.Collections.Generic;
using System.Threading.Tasks;

namespace NFTMarketServer.Message.Provider;

public class MessageInfoProvider : IMessageInfoProvider
{
    public Task<List<MessageInfoDto>> GetUserMessageInfosAsync(string address)
    {
        throw new System.NotImplementedException();
    }
}