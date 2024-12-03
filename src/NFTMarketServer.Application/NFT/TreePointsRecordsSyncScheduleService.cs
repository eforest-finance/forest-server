using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.HandleException;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using NFTMarketServer.TreeGame.Provider;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace NFTMarketServer.NFT;

public class TreePointsRecordsSyncScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly ITreeGamePointsRecordProvider _treeGamePointsRecordProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private const int HeightExpireMinutes = 5;
    
    public TreePointsRecordsSyncScheduleService(ILogger<NFTActivitySyncScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        ITreeGamePointsRecordProvider treeGamePointsRecordProvider,
        IDistributedEventBus distributedEventBus,
        IObjectMapper objectMapper,
        IDistributedCache<List<string>> distributedCache,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _treeGamePointsRecordProvider = treeGamePointsRecordProvider;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _distributedCache = distributedCache;
    }
    [ExceptionHandler(typeof(Exception),
        Message = "TreePointsRecordsSyncScheduleService.GetSyncTreePointsRecordsAsync is fail", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new []{"chainId", "lastEndHeight", "newIndexHeight" }
    )]
    public virtual async Task<IndexerTreePointsRecordPage> GetSyncTreePointsRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        return await _treeGamePointsRecordProvider.GetSyncTreePointsRecordsAsync(lastEndHeight,
            newIndexHeight, chainId);
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        long maxProcessedBlockHeight = -1;
        //Paging for logical processing
        var changePageInfo = new IndexerTreePointsRecordPage();
        changePageInfo = await GetSyncTreePointsRecordsAsync(chainId, lastEndHeight, newIndexHeight);

        if (changePageInfo.TotalRecordCount ==0 || changePageInfo.TreePointsChangeRecordList.IsNullOrEmpty())
        {
            _logger.LogInformation(
                "HandleTreePointsRecordAsync no data skipCount={A} lastEndHeight={B}", skipCount,
                lastEndHeight);
            return 0;
        }
        var processChangeOriginList = changePageInfo.TreePointsChangeRecordList;
        
        _logger.LogInformation(
            "HandleTreePointsRecordAsync queryOriginList lastEndHeight: {lastHeight} queryList count{count},chainId:{chainId} ",
            lastEndHeight, processChangeOriginList.Count, chainId);
        
        var blockHeight = await HandleTreePointsRecordAsync(chainId, processChangeOriginList, lastEndHeight);

        maxProcessedBlockHeight = Math.Max(maxProcessedBlockHeight, blockHeight);
        
        return maxProcessedBlockHeight;
    }
    
    private async Task<long> HandleTreePointsRecordAsync(string chainId,
        List<TreePointsChangeRecordItem> treePointsRecordList, long lastEndHeight)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();
        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        var recordList = await _distributedCache.GetAsync(cacheKey);
        foreach (var record in treePointsRecordList)
        {
            var innerKey = record.Id;
            if (recordList != null && recordList.Contains(innerKey))
            {
                _logger.LogDebug("HandleTreePointsRecordAsync duplicated bizKey: {A}", record.Id);
                continue;
            }
            
            blockHeight = Math.Max(blockHeight, record.BlockHeight);
            stopwatch.Start();
            await TreePointsRecordSignalAsync(record);
        
            stopwatch.Stop();
            _logger.LogInformation(
                "It took {Elapsed} ms to execute TreePointsRecordsSyncScheduleService for record: {B}.",
                stopwatch.ElapsedMilliseconds, record.Id);

        }
        if (blockHeight > 0)
        {
            recordList = treePointsRecordList.Where(obj => obj.BlockHeight == blockHeight)
                .Select(obj => obj.Id)
                .ToList();
            await _distributedCache.SetAsync(cacheKey, recordList,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
                });
        }

        return blockHeight;
    }

    private async Task TreePointsRecordSignalAsync(TreePointsChangeRecordItem item)
    {
        await _distributedEventBus.PublishAsync(new TreePointsChangeRecordEto
        {
            TreePointsChangeRecordItem = item
        });
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.TreePointsRecordsSync;
    }
}