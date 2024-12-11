using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.Grains.Grain.Synchronize.Ai;
using NFTMarketServer.HandleException;
using NFTMarketServer.NFT.Index;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Synchronize;

public interface ISynchronizeAITokenAppService
{
    Task<List<string>> SearchUnfinishedSynchronizeAITokenAsync();
    Task ExecuteJobAsync(string symbol);
}

public class SynchronizeAITokenAppService : ISynchronizeAITokenAppService, ISingletonDependency,
    ITransientDependency
{
    private readonly ILogger<SynchronizeAITokenAppService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<SynchronizeAITokenJobInfoIndex, string> _synchronizeAITokenJobInfoIndexRepository;

    public SynchronizeAITokenAppService(
        ILogger<SynchronizeAITokenAppService> logger,
        IClusterClient clusterClient,
        IObjectMapper objectMapper,
        INESTRepository<SynchronizeAITokenJobInfoIndex, string> synchronizeAITokenJobInfoIndexRepository)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _synchronizeAITokenJobInfoIndexRepository = synchronizeAITokenJobInfoIndexRepository;
    }

    public async Task<List<string>> SearchUnfinishedSynchronizeAITokenAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SynchronizeAITokenJobInfoIndex>, QueryContainer>>()
        {
            q => q.Match(m => m.Field(f => f.Status).Query(CrossCreateAITokenStatus.Failed)),
            q => q.Match(m => m.Field(f => f.Status).Query(CrossCreateAITokenStatus.CrossChainTokenCreated))
        };

        QueryContainer Filter(QueryContainerDescriptor<SynchronizeAITokenJobInfoIndex> f) =>
            f.Bool(b => b.MustNot(mustQuery));

        var list = await _synchronizeAITokenJobInfoIndexRepository.GetListAsync(Filter);
        _logger.LogInformation(
            "There are {COUNT} ai token that have not completed the synchronization", list.Item1);

        return list.Item1 < 1 ? new List<string>() : list.Item2.Select(o => o.Symbol).ToList();
    }

    [ExceptionHandler(typeof(Exception),
        Message = "SynchronizeAITokenAppService ExecuteJobAsync execution",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new[] { "symbol" }
    )]
    public virtual async Task ExecuteJobAsync(string symbol)
    {
        var syncTxEsData = await SearchSynchronizeAITokenJobInfoIndexAsync(symbol);
        if (syncTxEsData == null)
        {
            return;
        }

        var synchronizeAiTokenJobGrain = _clusterClient.GetGrain<ISynchronizeAITokenJobGrain>(symbol);
        var result = await synchronizeAiTokenJobGrain.ExecuteJobAsync(
            _objectMapper.Map<SynchronizeAITokenJobInfoIndex, SynchronizeAITokenJobGrainDto>(syncTxEsData));
        if (result == null)
        {
            _logger.LogError("synchronizeAiTokenJobGrain.ExecuteJobAsync fail");
            return;
        }

        _logger.LogInformation(
            "Execute sync ai token job in grain successfully, ready to update {symbol} {status}", symbol,
            result.Data.Status);

        if (syncTxEsData.Status == result.Data.Status) return;

        var infoIndex =
            _objectMapper.Map<SynchronizeAITokenJobGrainDto, SynchronizeAITokenJobInfoIndex>(result.Data);
        await _synchronizeAITokenJobInfoIndexRepository.AddOrUpdateAsync(infoIndex);
    }

    private async Task<SynchronizeAITokenJobInfoIndex> SearchSynchronizeAITokenJobInfoIndexAsync(string symbol)
    {
        var result = await _synchronizeAITokenJobInfoIndexRepository.GetAsync(symbol);
        if (result == null)
        {
            return null;
        }

        return result;
    }
}