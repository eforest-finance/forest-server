using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Eto;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.Hubs;

public class MessageChangeHandler : IDistributedEventHandler<MessageChangeEto>, ISingletonDependency
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

    public async Task HandleEventAsync(MessageChangeEto eventData)
    {
        try
        {
            if (eventData == null || eventData.Address.IsNullOrEmpty())
            {
                _logger.LogError("MessageChangeHandler param is null");
                return;
            }

            _logger.LogInformation(
                "MessageChangeHandler: {groupName}, address:{Bidder},nftInfoId:{NFTInfoId}, chainid:{ChainId} start time {time}",
                _marketHubGroupProvider.QueryMethodNameForReceiveMessageChangeSignal()
                , eventData.Address, DateTime.Now.ToString());


            var groupName =
                _marketHubGroupProvider.QueryNameForReceiveMessageChangeSignal(eventData.Address);
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