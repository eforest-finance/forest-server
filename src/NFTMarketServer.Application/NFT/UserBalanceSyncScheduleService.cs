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
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using NFTMarketServer.Users;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT;

public class UserBalanceSyncScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly IUserBalanceProvider _userBalanceProvider;

    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private const int HeightExpireMinutes = 5;

    public UserBalanceSyncScheduleService(ILogger<UserBalanceSyncScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        IUserBalanceProvider userBalanceProvider,
        IDistributedEventBus distributedEventBus,
        IObjectMapper objectMapper,
        IDistributedCache<List<string>> distributedCache,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _userBalanceProvider = userBalanceProvider;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _distributedCache = distributedCache;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        long maxProcessedBlockHeight = -1;
        //Paging for logical processing
        var changePageInfo = await _userBalanceProvider.QueryUserBalanceListAsync(new QueryUserBalanceInput()
            {
                SkipCount = skipCount,
                BlockHeight = lastEndHeight,
                ChainId = chainId
            });

        if (changePageInfo == null || changePageInfo.Data.IsNullOrEmpty())
        {
            _logger.LogInformation(
                "graphql QueryUserBalanceListAsync no data skipCount={A} lastEndHeight={B}", skipCount,
                lastEndHeight);
            return 0;
        }
        var processChangeOriginList = changePageInfo.Data;
        
        _logger.LogInformation(
            "graphql QueryUserBalanceListAsync count: {count} queryList count{count},chainId:{chainId} ",
            processChangeOriginList.Count, processChangeOriginList.Count, chainId);
        
        var blockHeight = await HandleUserBalanceAsync(chainId, processChangeOriginList, lastEndHeight);

        maxProcessedBlockHeight = Math.Max(maxProcessedBlockHeight, blockHeight);
        
        return maxProcessedBlockHeight;
    }
    
    private async Task<long> HandleUserBalanceAsync(string chainId,
        List<UserBalanceDto> userBalanceDtos, long lastEndHeight)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();
        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        var balanceList = await _distributedCache.GetAsync(cacheKey);
        foreach (var userBalance in userBalanceDtos)
        {
            var innerKey = userBalance.Id + userBalance.BlockHeight;
            if (balanceList != null && balanceList.Contains(innerKey))
            {
                _logger.Debug("HandleUserBalanceAsync duplicated bizKey: {A}", userBalance.Id);
                continue;
            }
            
            blockHeight = Math.Max(blockHeight, userBalance.BlockHeight);
            stopwatch.Start();
            await UserBalanceSignalAsync(userBalance);
            stopwatch.Stop();
            _logger.LogInformation(
                "It took {Elapsed} ms to execute HandleUserBalanceAsync for symbol ChainId:{chainId} bizId: {A} blockHeight: {B}.",
                stopwatch.ElapsedMilliseconds, chainId, userBalance.Id, userBalance.BlockHeight);

        }
        if (blockHeight > 0)
        {
            balanceList = userBalanceDtos.Where(obj => obj.BlockHeight == blockHeight)
                .Select(obj => obj.Id + obj.BlockHeight)
                .ToList();
            await _distributedCache.SetAsync(cacheKey, balanceList,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
                });
        }

        if (lastEndHeight == blockHeight)
        {
            blockHeight += 1;
        }
        return blockHeight;
    }

    private async Task UserBalanceSignalAsync(UserBalanceDto item)
    {
        if (item == null)
        {
            return;
        }

        await _distributedEventBus.PublishAsync(new UserBalanceEto()
        {
            UserBalanceDto = item
        });
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.UserBalanceSync;
    }
}