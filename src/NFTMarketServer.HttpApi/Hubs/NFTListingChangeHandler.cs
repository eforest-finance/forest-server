using System;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Eto;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Hubs;

public class NFTListingChangeHandler : IConsumer<NewIndexEvent<NFTListingChangeEto>>, ITransientDependency
{
    private readonly ILogger<NFTListingChangeHandler> _logger;
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IOptionsMonitor<ChoiceNFTInfoNewFlagOptions>
        _choiceNFTInfoNewFlagOptionsMonitor;

    public NFTListingChangeHandler(
        IHubContext<MarketHub> hubContext,
        IMarketHubGroupProvider marketHubGroupProvider,
        INFTInfoAppService nftInfoAppService,
        IOptionsMonitor<ChoiceNFTInfoNewFlagOptions> choiceNFTInfoNewFlagOptionsMonitor,
        ILogger<NFTListingChangeHandler> logger)
    {
        _logger = logger;
        _hubContext = hubContext;
        _nftInfoAppService = nftInfoAppService;
        _marketHubGroupProvider = marketHubGroupProvider;
        _choiceNFTInfoNewFlagOptionsMonitor = choiceNFTInfoNewFlagOptionsMonitor;
    }
    
    public async Task Consume(ConsumeContext<NewIndexEvent<NFTListingChangeEto>> eventData)
    {
        try
        {
            var nftListingChange = eventData.Message.Data;

            if (!SymbolHelper.CheckSymbolIsNFTInfoId(nftListingChange.NftId))
            {
                _logger.Debug("NFTListingChangeHandler  nftInfoId is not common nft {NFTInfoId}",nftListingChange.NftId);
                return;
            }
            
            _logger.LogInformation(
                "NFTListingChangeHandler: {groupName}, symbol:{Bidder},nftInfoId:{NFTInfoId}, chainid:{ChainId} start time {time}",
                _marketHubGroupProvider.QueryMethodNameForReceiveListingChangeSignal()
                , eventData.Message.Data.Symbol,eventData.Message.Data.NftId,eventData.Message.Data.ChainId, DateTime.Now.ToString());
            
            
            var choiceNFTInfoNewFlag = _choiceNFTInfoNewFlagOptionsMonitor?.CurrentValue?
                .ChoiceNFTInfoNewFlagIsOn ?? false;

            if (choiceNFTInfoNewFlag && nftListingChange.NftId != null && nftListingChange.ChainId != null)
            {
                await _nftInfoAppService.AddOrUpdateNftInfoNewByIdAsync(nftListingChange.NftId,nftListingChange.ChainId);
            }
            
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