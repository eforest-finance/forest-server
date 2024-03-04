using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Basic;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Orleans.Runtime;
using Orleans.Runtime.Configuration;
using Volo.Abp.Caching;

namespace NFTMarketServer.NFT;

public class NftInfoNewSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IChainAppService _chainAppService;
    private const int HeightExpireMinutes = 5;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private readonly IDistributedCache<string> _distributedCacheForHeight;
    private readonly INFTTraitProvider _inftTraitProvider;

    public NftInfoNewSyncDataService(ILogger<NftInfoNewSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        INFTInfoAppService nftInfoAppService,
        IDistributedCache<List<string>> distributedCache,
        IDistributedCache<string> distributedCacheForHeight,
        INFTTraitProvider inftTraitProvider,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _nftInfoAppService = nftInfoAppService;
        _chainAppService = chainAppService;
        _distributedCache = distributedCache;
        _distributedCacheForHeight = distributedCacheForHeight;
        _inftTraitProvider = inftTraitProvider;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        try
        {
            var resetSyncHeightFlagMinutesStr =
                await _distributedCacheForHeight.GetAsync(CommonConstant.ResetNFTNewSyncHeightFlagCacheKey);
            var nftResetHeightFlagCacheValue =
                await _distributedCacheForHeight.GetAsync(CommonConstant.NFTNewResetHeightFlagCacheKey + chainId);
            _logger.Debug(
                "GetCompositeNFTInfosAsync nft {ResetSyncHeightFlag} {NFTSyncHeightFlag} {nftSyncHeightFlagMinuteStr}",
                resetSyncHeightFlagMinutesStr, CommonConstant.NFTNewResetHeightFlagCacheKey,
                resetSyncHeightFlagMinutesStr);
            if (!resetSyncHeightFlagMinutesStr.IsNullOrEmpty())
            {
                if (nftResetHeightFlagCacheValue.IsNullOrEmpty())
                {
                    var resetNftSyncHeightExpireMinutes = int.Parse(resetSyncHeightFlagMinutesStr);

                    await _distributedCacheForHeight.SetAsync(CommonConstant.NFTNewResetHeightFlagCacheKey + chainId,
                        CommonConstant.NFTNewResetHeightFlagCacheKey, new DistributedCacheEntryOptions
                        {
                            AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(resetNftSyncHeightExpireMinutes)
                        });

                    return CommonConstant.BeginHeight;
                }
            }
        }
        catch (Exception e)
        {
            _logger.Error(CommonConstant.IntError, "Something is wrong for SyncIndexerRecordsAsync reset height", e);
        }

        var queryList = await _graphQlProvider.GetSyncNftInfoRecordsAsync(chainId, lastEndHeight, 0);
        _logger.LogInformation(
            "SyncNftInfoRecords queryList startBlockHeight: {lastEndHeight} endBlockHeight: {newIndexHeight} count: {count}",
            lastEndHeight, newIndexHeight, queryList?.Count);
        long blockHeight = -1;
        if (queryList.IsNullOrEmpty())
        {
            return 0;
        }

        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        List<string> symbolList = await _distributedCache.GetAsync(cacheKey);
        foreach (var nftInfo in queryList)
        {
            if (nftInfo == null) continue;
            var innerKey = nftInfo.Symbol + nftInfo.BlockHeight;
            if (symbolList != null && symbolList.Contains(innerKey))
            {
                _logger.Debug("GetSyncNftInfoRecordsAsync duplicated symbol: {symbol}", nftInfo.Symbol);
                continue;
            }

            blockHeight = Math.Max(blockHeight, nftInfo.BlockHeight);
            var nftInfoNewIndex = await _nftInfoAppService.AddOrUpdateNftInfoNewAsync(nftInfo, nftInfo.Id, chainId);
            if (nftInfoNewIndex != null && !nftInfoNewIndex.TraitPairsDictionary.IsNullOrEmpty())
            {
                await _inftTraitProvider.CheckAndUpdateTraitInfo(nftInfoNewIndex);
            }
        }

        if (blockHeight > 0)
        {
            symbolList = queryList.Where(obj => obj.BlockHeight == blockHeight)
                .Select(obj => obj.Symbol + obj.BlockHeight)
                .ToList();
            await _distributedCache.SetAsync(cacheKey, symbolList,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
                });
        }

        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(CommonConstant.IntOne);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.NftInfoNewSync;
    }
}