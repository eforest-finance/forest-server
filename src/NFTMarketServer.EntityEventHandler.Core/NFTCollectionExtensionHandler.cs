using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTCollectionExtensionHandler : IDistributedEventHandler<NFTCollectionExtraEto>, ITransientDependency
{
    private readonly INESTRepository<NFTCollectionExtensionIndex, string> _nftCollectionExtensionRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NFTCollectionExtensionHandler> _logger;
    private readonly IDistributedEventBus _distributedEventBus;

    public NFTCollectionExtensionHandler(
        INESTRepository<NFTCollectionExtensionIndex, string> nftCollectionExtensionRepository,
        IObjectMapper objectMapper,
        IDistributedEventBus distributedEventBus, 
        ILogger<NFTCollectionExtensionHandler> logger)
    {
        _nftCollectionExtensionRepository = nftCollectionExtensionRepository;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "NFTCollectionExtensionHandler.HandleEventAsync nftCollectionExtension information add or update fail", 
        TargetType = typeof(ExceptionHandlingService), 
        LogOnly = true,
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"eventData" }
    )]
    public virtual async Task HandleEventAsync(NFTCollectionExtraEto eventData)
    {
        var nftCollectionExtensionIndex = new NFTCollectionExtensionIndex();
        var originInfo = await _nftCollectionExtensionRepository.GetAsync(eventData.Id);
        if (originInfo != null)
        {
            nftCollectionExtensionIndex = _objectMapper.Map(eventData,originInfo);
        }
        else
        {
            nftCollectionExtensionIndex =
                _objectMapper.Map<NFTCollectionExtraEto, NFTCollectionExtensionIndex>(eventData);
        }
            
        await _nftCollectionExtensionRepository.AddOrUpdateAsync(nftCollectionExtensionIndex);

        _logger.LogDebug("nftCollectionExtension information add or update success: {NftCollectionExtensionIndex}",
            JsonConvert.SerializeObject(nftCollectionExtensionIndex));

        var collectionId = eventData.Id;
        var chainId = eventData.ChainId;
        var utcHourStartTimestamp = TimeHelper.GetUtcHourStartTimestamp();
        var utcHourStart = TimeHelper.FromUnixTimestampSeconds(utcHourStartTimestamp);
        var utcHourStartStr = TimeHelper.GetDateTimeFormatted(utcHourStart);
        await _distributedEventBus.PublishAsync(new NFTCollectionTradeEto
        {
            Id = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId, utcHourStartStr),
            CollectionId = collectionId,
            ChainId = chainId,
            CurrentOrdinal = utcHourStartTimestamp,
            CurrentOrdinalStr = utcHourStartStr
        });
    }
}