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

public class NewMessageChangeHandler : IConsumer<NewIndexEvent<MessageChangeEto>>, ITransientDependency
{
    private readonly ILogger<NewMessageChangeHandler> _logger;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;

    public NewMessageChangeHandler(
        IHubContext<MarketHub> hubContext,
        IMarketHubGroupProvider marketHubGroupProvider,
        ILogger<NewMessageChangeHandler> logger)
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
            var groupName =
                _marketHubGroupProvider.QueryNameForReceiveMessageChangeSignal(eventData.Message.Data.Address);

            _logger.LogInformation(
                "MessageChangeHandler: {groupName}, address:{Bidder} start time {time}",
                groupName
                , eventData.Message.Data.Address, DateTime.Now.ToString());

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