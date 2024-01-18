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

public class NFTListingChangeHandler : IConsumer<NewIndexEvent<NFTListingChangeEto>>, ITransientDependency
{
    private readonly ILogger<NFTListingChangeHandler> _logger;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;

    public NFTListingChangeHandler(
        IHubContext<MarketHub> hubContext,
        IMarketHubGroupProvider marketHubGroupProvider,
        ILogger<NFTListingChangeHandler> logger)
    {
        _logger = logger;
        _hubContext = hubContext;
        _marketHubGroupProvider = marketHubGroupProvider;
    }
    
    public async Task Consume(ConsumeContext<NewIndexEvent<NFTListingChangeEto>> eventData)
    {
        try
        {
            _logger.LogInformation(
                "NFTListingChangeHandler: {groupName}, symbol:{Bidder}, start time {time}",
                _marketHubGroupProvider.QueryMethodNameForReceiveListingChangeSignal()
                , eventData.Message.Data.Symbol, DateTime.Now.ToString());
            
            var groupName =
                _marketHubGroupProvider.QueryNameForReceiveListingChangeSignal(eventData.Message.Data.Symbol);
            var signal = new ChangeSignalBaseDto
            {
                HasChanged = true
            };
            await _hubContext.Clients.Group(groupName).SendAsync(_marketHubGroupProvider.QueryMethodNameForReceiveListingChangeSignal(), signal);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NFTListingChangeHandler fail: {Data}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}