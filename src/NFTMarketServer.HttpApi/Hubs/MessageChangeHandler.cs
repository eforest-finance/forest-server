using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Eto;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Hubs;

public class MessageChangeHandler : IConsumer<NewIndexEvent<MessageChangeEto>>, ITransientDependency
{
    private readonly ILogger<MessageChangeHandler> _logger;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;

    public MessageChangeHandler(
        IHubContext<MarketHub> hubContext,
        IMarketHubGroupProvider marketHubGroupProvider,
        ILogger<MessageChangeHandler> logger)
    {
        _logger = logger;
        _hubContext = hubContext;
        _marketHubGroupProvider = marketHubGroupProvider;
    }

    public async Task Consume(ConsumeContext<NewIndexEvent<MessageChangeEto>> eventData)
    {
        try
        {
            _logger.LogInformation("MessageChangeHandler eventData={A}", JsonConvert.SerializeObject(eventData));
            if (eventData == null || eventData.Message.Data.Address.IsNullOrEmpty())
            {
                _logger.LogError("MessageChangeHandler param is null");
                return;
            }

            _logger.LogInformation(
                "MessageChangeHandler: {groupName}, address:{Bidder},nftInfoId:{NFTInfoId}, chainid:{ChainId} start time {time}",
                _marketHubGroupProvider.QueryMethodNameForReceiveMessageChangeSignal()
                , eventData.Message.Data.Address, DateTime.Now.ToString());


            var groupName =
                _marketHubGroupProvider.QueryNameForReceiveMessageChangeSignal(eventData.Message.Data.Address);
            var signal = new ChangeSignalBaseDto
            {
                HasChanged = true
            };
            await _hubContext.Clients.Group(groupName)
                .SendAsync(_marketHubGroupProvider.QueryMethodNameForReceiveMessageChangeSignal(), signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "MessageChangeHandler fail: {Data}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}