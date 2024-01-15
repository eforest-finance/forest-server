using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Synchronize.Eto;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTSynchronizeHandler : IDistributedEventHandler<SynchronizeTransactionInfoEto>, ITransientDependency
{
    private readonly INESTRepository<SynchronizeTransactionInfoIndex, string> _synchronizeTransactionInfoRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NFTSynchronizeHandler> _logger;

    public NFTSynchronizeHandler(
        INESTRepository<SynchronizeTransactionInfoIndex, string> synchronizeTransactionInfoRepository,
        IObjectMapper objectMapper,
        ILogger<NFTSynchronizeHandler> logger)
    {
        _synchronizeTransactionInfoRepository = synchronizeTransactionInfoRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task HandleEventAsync(SynchronizeTransactionInfoEto eventData)
    {
        try
        {
            var syncTx = _objectMapper.Map<SynchronizeTransactionInfoEto, SynchronizeTransactionInfoIndex>(eventData);

            await _synchronizeTransactionInfoRepository.AddOrUpdateAsync(syncTx);

            _logger.LogInformation("Transaction {txHash} add or update success.", eventData.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while processing the event,txHash {txHash}", eventData.Id);
            throw new Exception($"An error occurred while processing the event,txHash {eventData.Id}", ex);
        }
    }
}