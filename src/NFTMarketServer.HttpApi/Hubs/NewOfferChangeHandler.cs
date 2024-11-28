using System.Threading.Tasks;
using MassTransit;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.NFT;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Hubs;

public class NewOfferChangeHandler : IConsumer<NewIndexEvent<NFTOfferChangeDto>>, ITransientDependency
{
    private readonly IHubContext<MarketHub> _hubContext;
    private readonly IMarketHubGroupProvider _marketHubGroupProvider;
    private readonly ILogger<NewOfferChangeHandler> _logger;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IOptionsMonitor<ChoiceNFTInfoNewFlagOptions>
        _choiceNFTInfoNewFlagOptionsMonitor;

    public NewOfferChangeHandler(IHubContext<MarketHub> hubContext, IMarketHubGroupProvider marketHubGroupProvider,
        INFTInfoAppService nftInfoAppService,
        IOptionsMonitor<ChoiceNFTInfoNewFlagOptions> choiceNFTInfoNewFlagOptionsMonitor,
        ILogger<NewOfferChangeHandler> logger)
    {
        _hubContext = hubContext;
        _marketHubGroupProvider = marketHubGroupProvider;
        _nftInfoAppService = nftInfoAppService;
        _choiceNFTInfoNewFlagOptionsMonitor = choiceNFTInfoNewFlagOptionsMonitor;
        _logger = logger;
    }
    
    public async Task Consume(ConsumeContext<NewIndexEvent<NFTOfferChangeDto>> eventData)
    {
        var nftOfferChangeDto = eventData.Message.Data;
        if (!SymbolHelper.CheckSymbolIsNFTInfoId(nftOfferChangeDto.NftId))
        {
            _logger.LogDebug("NewOfferChangeHandler  nftInfoId is not common nft {NFTInfoId}",nftOfferChangeDto.NftId);
            return;
        }
        
        var choiceNFTInfoNewFlag = _choiceNFTInfoNewFlagOptionsMonitor?.CurrentValue?
            .ChoiceNFTInfoNewFlagIsOn ?? false;
        if (choiceNFTInfoNewFlag && nftOfferChangeDto.NftId != null && nftOfferChangeDto.ChainId != null)
        {
            await _nftInfoAppService.AddOrUpdateNftInfoNewByIdAsync(nftOfferChangeDto.NftId,nftOfferChangeDto.ChainId);
        }
        
        var groupName = _marketHubGroupProvider.GetNtfOfferChangeGroupName(nftOfferChangeDto.NftId);
        
        await _hubContext.Clients.Group(groupName).SendAsync("ReceiveOfferChangeSignal", new NFTOfferChangeSignalDto
        {
            hasChanged = true
        });
        _logger.LogInformation(
            "ReceiveOfferChangeSignal: {groupName}, chainId:{id}, NFTInfoIf:{NFTInfoId} blockHeight:{blockHeight}",
            groupName, nftOfferChangeDto.ChainId, nftOfferChangeDto.NftId, nftOfferChangeDto.BlockHeight);


    }
}