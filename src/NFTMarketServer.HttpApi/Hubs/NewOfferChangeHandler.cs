using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using NFTMarketServer.NFT;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Hubs;

public class NewOfferChangeHandler : IConsumer<NewIndexEvent<NFTOfferChangeDto>>, ITransientDependency
{
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;
    private readonly ILogger<NewOfferChangeHandler> _logger;

    public NewOfferChangeHandler(IHubContext<MarketHub> hubContext, IMarketHubGroupProvider marketHubGroupProvider,
        ILogger<NewOfferChangeHandler> logger)
    {
        _hubContext = hubContext;
        _marketHubGroupProvider = marketHubGroupProvider;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<NewIndexEvent<NFTOfferChangeDto>> eventData)
    {
        var nftOfferChangeDto = eventData.Message.Data;
        var groupName = _marketHubGroupProvider.GetNtfOfferChangeGroupName(nftOfferChangeDto.NftId);
        
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveOfferChangeSignal", new NFTOfferChangeSignalDto
        {
            hasChanged = true
        });
        _logger.LogInformation("ReceiveOfferChangeSignal: {groupName}, chainId:{id}, blockHeight:{blockHeight}", 
            groupName, nftOfferChangeDto.ChainId, nftOfferChangeDto.BlockHeight);
    }
}