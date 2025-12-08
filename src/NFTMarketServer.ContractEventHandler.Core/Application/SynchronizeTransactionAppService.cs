using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.Grain.Synchronize;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Synchronize.Dto;
using NFTMarketServer.Synchronize.Eto;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.ContractEventHandler.Core.Application;

public interface ISynchronizeTransactionAppService
{
    Task<List<string>> SearchUnfinishedSynchronizeTransactionAsync();
    Task ExecuteJobAsync(string txHash);
}

public class SynchronizeTransactionAppService : ISynchronizeTransactionAppService, ISingletonDependency,
    ITransientDependency
{
    private readonly INESTRepository<SynchronizeTransactionInfoIndex, string> _synchronizeTransactionRepository;
    private readonly ILogger<SynchronizeTransactionAppService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;


    public SynchronizeTransactionAppService(
        INESTRepository<SynchronizeTransactionInfoIndex, string> synchronizeTransactionRepository,
        ILogger<SynchronizeTransactionAppService> logger, IClusterClient clusterClient, IObjectMapper objectMapper,
        IDistributedEventBus distributedEventBus)
    {
        _synchronizeTransactionRepository = synchronizeTransactionRepository;
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<List<string>> SearchUnfinishedSynchronizeTransactionAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SynchronizeTransactionInfoIndex>, QueryContainer>>()
        {
            q => q.Match(m => m.Field(f => f.Status).Query(SynchronizeTransactionJobStatus.Failed)),
            q => q.Match(m => m.Field(f => f.Status).Query(SynchronizeTransactionJobStatus.CrossChainTokenCreated)),
            q => q.Match(m => m.Field(f => f.Status).Query(SynchronizeTransactionJobStatus.AuctionCreated))
        };

        QueryContainer Filter(QueryContainerDescriptor<SynchronizeTransactionInfoIndex> f) =>
            f.Bool(b => b.MustNot(mustQuery));

        var (_, synchronizeTransactions) = await _synchronizeTransactionRepository.GetListAsync(Filter);

        var newList = synchronizeTransactions.Where(x => x.Status != null && x.TxHash != null).ToList();

        _logger.LogInformation(
            "There are {COUNT} transactions that have not completed the synchronization transaction", newList.Count);

        return newList.Count < 1 ? new List<string>() : newList.Select(o => o.TxHash).ToList();
    }

    public async Task ExecuteJobAsync(string txHash)
    {
        var syncTxEsData = await SearchSynchronizeTransactionByTxHashAsync(txHash);

        var synchronizeTransactionJobGrain = _clusterClient.GetGrain<ISynchronizeTxJobGrain>(txHash);
        var result = await synchronizeTransactionJobGrain.ExecuteJobAsync(
            _objectMapper.Map<SynchronizeTransactionInfoEto, SynchronizeTxJobGrainDto>(syncTxEsData));

        _logger.LogInformation(
            "Execute transaction job in grain successfully, ready to update {txHash} {status}", txHash,
            result.Data.Status);

        if (syncTxEsData.Status == result.Data.Status) return;

        var syncTxEtoData =
            _objectMapper.Map<SynchronizeTxJobGrainDto, SynchronizeTransactionInfoEto>(result.Data);
        syncTxEtoData.UserId = syncTxEsData.UserId;
        syncTxEtoData.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();

        await _distributedEventBus.PublishAsync(syncTxEtoData);
    }

    private async Task<SynchronizeTransactionInfoEto> SearchSynchronizeTransactionByTxHashAsync(string txHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SynchronizeTransactionInfoIndex>, QueryContainer>>() { };
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.TxHash).Terms(txHash)));

        QueryContainer Filter(QueryContainerDescriptor<SynchronizeTransactionInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, syncTxs) = await _synchronizeTransactionRepository.GetListAsync(Filter);

        return totalCount < 1
            ? new SynchronizeTransactionInfoEto()
            : _objectMapper.Map<SynchronizeTransactionInfoIndex, SynchronizeTransactionInfoEto>(syncTxs.First());
    }
}