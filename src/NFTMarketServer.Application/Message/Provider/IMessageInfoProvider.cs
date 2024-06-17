using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace NFTMarketServer.Message.Provider;

public interface IMessageInfoProvider
{
    public Task<Tuple<long, List<MessageInfoIndex>>> GetUserMessageInfosAsync(string address, QueryMessageListInput input);

}