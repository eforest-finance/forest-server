using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.NFT;

public class NFTCollectionChangeService : NFTMarketServerAppService, INFTCollectionChangeService
{

    private const string CachekeyPreifix = "NFTCollectionChange";
    private readonly ILogger<NFTCollectionChangeService> _logger;
    private readonly INFTCollectionProvider _nftCollectionProvider;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private const int HeightExpireMinutes = 5;
    private readonly INFTCollectionProviderAdapter _nftCollectionProviderAdapter;
    private readonly IDistributedEventBus _distributedEventBus;

    public NFTCollectionChangeService(ILogger<NFTCollectionChangeService> logger,
        INFTCollectionProvider nftCollectionProvider,
        IDistributedCache<List<string>> distributedCache,
        INFTCollectionProviderAdapter nftCollectionProviderAdapter,
        IDistributedEventBus distributedEventBus)
    {
        _logger = logger;
        _nftCollectionProvider = nftCollectionProvider;
        _distributedCache = distributedCache;
        
        _nftCollectionProvider = nftCollectionProvider;
        _nftCollectionProviderAdapter = nftCollectionProviderAdapter;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<long> HandleItemsChangesAsync(string chainId, List<IndexerNFTCollectionChange> collectionChanges)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();
        try
        {
            
            foreach (var collectionChange in collectionChanges)
            {
                //mark maxProcessedBlockHeight
                blockHeight = Math.Max(blockHeight, collectionChange.BlockHeight);
                stopwatch.Start();
                var collectionId =
                    IdGenerateHelper.GetNFTCollectionId(collectionChange.ChainId, collectionChange.Symbol);
                var nftCollectionExtension =
                    await _nftCollectionProvider.GenerateNFTCollectionExtensionById(chainId, collectionChange.Symbol);
                stopwatch.Stop();
                _logger.LogInformation(
                    "It took {Elapsed} ms to execute GetNFTCollectionExtensionByIdAsync for NFT Collection chainId:{chainId} symbol: {symbol}, collection extension data: {data}.",
                    stopwatch.ElapsedMilliseconds, chainId, collectionChange.Symbol,
                    JsonConvert.SerializeObject(nftCollectionExtension));
                var dto = new NFTCollectionExtensionDto
                {
                    Id = collectionId,
                    ItemTotal = nftCollectionExtension.ItemTotal,
                    OwnerTotal = nftCollectionExtension.OwnerTotal
                };
                await _nftCollectionProviderAdapter.AddOrUpdateNftCollectionExtensionAsync(dto);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HandleItemsChangesAsync failed collectionChanges {collectionChanges}",
                JsonConvert.SerializeObject(collectionChanges));
        }

        return blockHeight;
    }

    public async Task<long> HandlePriceChangesAsync(string chainId,
        List<IndexerNFTCollectionPriceChange> collectionChanges, long lastEndHeight,
        string businessType)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();

        try
        {
            var cacheKey = CachekeyPreifix + businessType + chainId + lastEndHeight;
            var symbolList = await _distributedCache.GetAsync(cacheKey);
            var changeFlag = false;
            foreach (var collectionChange in collectionChanges)
            {
                var innerKey = collectionChange.Symbol + collectionChange.BlockHeight;
                if (symbolList != null && symbolList.Contains(innerKey))
                {
                    _logger.Debug("GetNFTCollectionPriceAsync duplicated symbol: {symbol}", collectionChange.Symbol);
                    continue;
                }

                changeFlag = true;
                blockHeight = Math.Max(blockHeight, collectionChange.BlockHeight);
                stopwatch.Start();
                var collectionId =
                    IdGenerateHelper.GetNFTCollectionId(collectionChange.ChainId, collectionChange.Symbol);
                var collectionPrice =
                    await _nftCollectionProvider.GetNFTCollectionPriceAsync(chainId, collectionChange.Symbol, -1);
                stopwatch.Stop();
                _logger.LogInformation(
                    "It took {Elapsed} ms to execute GetNFTCollectionPriceAsync for NFT Collection ChainId:{chainId} Id: {Id} floorPrice: {data}.",
                    stopwatch.ElapsedMilliseconds, chainId, collectionId, collectionPrice.floorPrice);
                var dto = new NFTCollectionExtensionDto
                {
                    Id = collectionId,
                    FloorPrice = collectionPrice.floorPrice
                };
                await _nftCollectionProviderAdapter.AddOrUpdateNftCollectionExtensionAsync(dto);

                if (chainId.Equals(CommonConstant.MainChainId))
                {
                    continue;
                }

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

            if (changeFlag)
            {
                symbolList = collectionChanges.Where(obj => obj.BlockHeight == blockHeight)
                    .Select(obj => obj.Symbol + obj.BlockHeight)
                    .ToList();
                await _distributedCache.SetAsync(cacheKey, symbolList,
                    new DistributedCacheEntryOptions
                    {
                        AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
                    });
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HandlePriceChangesAsync failed collectionChanges {collectionChanges}",
                JsonConvert.SerializeObject(collectionChanges));
        }

        return blockHeight;
    }

    public async Task HandleCurrentInfoInitAsync(List<NFTCollectionExtensionIndex> collectionList)
    {
        foreach (var collection in collectionList)
        {
            if (collection.ChainId.Equals(CommonConstant.MainChainId))
            {
                continue;
            }
            var collectionId = collection.Id;
            var utcHourStartTimestamp = TimeHelper.GetUtcHourStartTimestamp();
            var utcHourStart = TimeHelper.FromUnixTimestampSeconds(utcHourStartTimestamp);
            var utcHourStartStr = TimeHelper.GetDateTimeFormatted(utcHourStart);
            await _distributedEventBus.PublishAsync(new NFTCollectionTradeEto
            {
                Id = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId, utcHourStartStr),
                CollectionId = collectionId,
                ChainId = collection.ChainId,
                CurrentOrdinal = utcHourStartTimestamp,
                CurrentOrdinalStr = utcHourStartStr
            });
        }
    }
}