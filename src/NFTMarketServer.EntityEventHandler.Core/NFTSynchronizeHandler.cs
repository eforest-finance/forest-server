using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Contracts.HandleException;
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
    [ExceptionHandler(typeof(Exception),
        Message = "NFTSynchronizeHandler.HandleEventAsync An error occurred while processing the event,txHash", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"eventData" }
    )]
    public virtual async Task HandleEventAsync(SynchronizeTransactionInfoEto eventData)
    {
        var syncTx = _objectMapper.Map<SynchronizeTransactionInfoEto, SynchronizeTransactionInfoIndex>(eventData);

        await _synchronizeTransactionInfoRepository.AddOrUpdateAsync(syncTx);

        _logger.LogInformation("Transaction {txHash} add or update success.", eventData.Id);
    }
}