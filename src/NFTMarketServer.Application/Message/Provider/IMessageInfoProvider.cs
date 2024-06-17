using System.Collections.Generic;
using System.Threading.Tasks;

namespace NFTMarketServer.Message.Provider;

public interface IMessageInfoProvider
{
    public Task<List<MessageInfoDto>> GetUserMessageInfosAsync(string address);

}