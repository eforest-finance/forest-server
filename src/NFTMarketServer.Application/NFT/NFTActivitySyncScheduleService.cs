using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using JsonConvert = Newtonsoft.Json.JsonConvert;

namespace NFTMarketServer.NFT;

public class NFTActivitySyncScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly INFTActivityProvider _nftActivityProvider;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private const int HeightExpireMinutes = 5;

    private static readonly List<int> ActivityTypeList = new List<int>{ EnumHelper.GetIndex(NFTActivityType.Mint),
        EnumHelper.GetIndex(NFTActivityType.Burn),
        EnumHelper.GetIndex(NFTActivityType.Sale),
        EnumHelper.GetIndex(NFTActivityType.ListWithFixedPrice),
        EnumHelper.GetIndex(NFTActivityType.DeList),
        EnumHelper.GetIndex(NFTActivityType.MakeOffer),
        EnumHelper.GetIndex(NFTActivityType.CancelOffer),
        EnumHelper.GetIndex(NFTActivityType.Sale),
        EnumHelper.GetIndex(NFTActivityType.PlaceBid),
        EnumHelper.GetIndex(NFTActivityType.CancelBid) };

    public NFTActivitySyncScheduleService(ILogger<NFTActivitySyncScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        INFTActivityProvider nftActivityProvider,
        IDistributedEventBus distributedEventBus,
        IObjectMapper objectMapper,
        IDistributedCache<List<string>> distributedCache,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _nftActivityProvider = nftActivityProvider;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _distributedCache = distributedCache;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        long maxProcessedBlockHeight = -1;
        //Paging for logical processing
        
        
        var changePageInfo = await _nftActivityProvider.GetMessageActivityListAsync(ActivityTypeList, skipCount,
            lastEndHeight, chainId);

        if (changePageInfo == null || changePageInfo.IndexerNftActivity.IsNullOrEmpty())
        {
            _logger.LogInformation(
                "HandleNFTActivityAsync no data skipCount={A} lastEndHeight={B} activityTypeList={C}", skipCount,
                lastEndHeight, JsonConvert.SerializeObject(ActivityTypeList));
            return 0;
        }
        var processChangeOriginList = changePageInfo.IndexerNftActivity;
        
        _logger.LogInformation(
            "HandleNFTActivityAsync queryOriginList count: {count} queryList count{count},chainId:{chainId} ",
            processChangeOriginList.Count, processChangeOriginList.Count, chainId);
        
        var blockHeight = await HandleNFTActivityAsync(chainId, processChangeOriginList, lastEndHeight);

        maxProcessedBlockHeight = Math.Max(maxProcessedBlockHeight, blockHeight);
        
        return maxProcessedBlockHeight;
    }
    
    private async Task<long> HandleNFTActivityAsync(string chainId,
        List<NFTActivityItem> nftActivityList, long lastEndHeight)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();
        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        var activityList = await _distributedCache.GetAsync(cacheKey);
        foreach (var nftActivity in nftActivityList)
        {
            var innerKey = nftActivity.Id+ nftActivity.Type + nftActivity.BlockHeight;
            if (activityList != null && activityList.Contains(innerKey))
            {
                _logger.LogDebug("HandleNFTActivityAsync duplicated bizKey: {A} Type={B}", nftActivity.Id,
                    nftActivity.Type);
                continue;
            }
            
            blockHeight = Math.Max(blockHeight, nftActivity.BlockHeight);
            stopwatch.Start();
            await NFTActivitySignalAsync(nftActivity);
            stopwatch.Stop();
            _logger.LogInformation(
                "It took {Elapsed} ms to execute HandleNFTActivityAsync for symbol ChainId:{chainId} bizId: {A} blockHeight: {B}.",
                stopwatch.ElapsedMilliseconds, chainId, nftActivity.Id, nftActivity.BlockHeight);

        }
        if (blockHeight > 0)
        {
            activityList = nftActivityList.Where(obj => obj.BlockHeight == blockHeight)
                .Select(obj => obj.Id + obj.Type + obj.BlockHeight)
                .ToList();
            await _distributedCache.SetAsync(cacheKey, activityList,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
                });
        }

        return blockHeight;
    }

    private async Task NFTActivitySignalAsync(NFTActivityItem item)
    {
        if (item == null)
        {
            return;
        }

        await _distributedEventBus.PublishAsync(new NFTActivitySyncEto
        {
            NFTActivitySyncDto = _objectMapper.Map<NFTActivityItem, NFTActivitySyncDto>(item)
        });
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.NFTActivitySync;
    }
}