using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Common;
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


    public async Task HandleEventAsync(NFTCollectionExtraEto eventData)
    {
        try
        {
            var nftCollectionExtensionIndex =
                _objectMapper.Map<NFTCollectionExtraEto, NFTCollectionExtensionIndex>(eventData);

            await _nftCollectionExtensionRepository.AddOrUpdateAsync(nftCollectionExtensionIndex);

            _logger.LogDebug("nftCollectionExtension information add or update success: {NftCollectionExtensionIndex}",
                JsonConvert.SerializeObject(nftCollectionExtensionIndex));

            var utcHourStartTimestamp = TimeHelper.GetUtcHourStartTimestamp();
            var utcHourStart = TimeHelper.FromUnixTimestampSeconds(utcHourStartTimestamp);
            var utcHourStartStr = TimeHelper.GetDateTimeFormatted(utcHourStart);
            await _distributedEventBus.PublishAsync(new NFTCollectionTradeEto
            {
                Id = IdGenerateHelper.GetHourlyCollectionTradeRecordId(eventData.Id, utcHourStartStr),
                CollectionId = eventData.Id,
                ChainId = eventData.ChainId,
                CurrentOrdinal = utcHourStartTimestamp,
                CurrentOrdinalStr = utcHourStartStr
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "nftCollectionExtension information add or update fail: {Data}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}