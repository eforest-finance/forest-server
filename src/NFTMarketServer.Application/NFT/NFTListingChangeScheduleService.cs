using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Orleans.Runtime;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT;

public class NFTListingChangeScheduleService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IChainAppService _chainAppService;
    private readonly INFTListingProvider _nftListingProvider;
    private readonly IBus _bus;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedCache<List<string>> _distributedCache;
    private const int HeightExpireMinutes = 5;

    public NFTListingChangeScheduleService(ILogger<NFTListingChangeScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        INFTListingProvider nftListingProvider,
        IBus bus,
        IObjectMapper objectMapper,
        IDistributedCache<List<string>> distributedCache,
        IChainAppService chainAppService)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _nftListingProvider = nftListingProvider;
        _bus = bus;
        _objectMapper = objectMapper;
        _distributedCache = distributedCache;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var skipCount = 0;
        long maxProcessedBlockHeight = -1;
        //Paging for logical processing
    
        var changePageInfo = await _nftListingProvider.GetIndexerNFTListingChangePageByBlockHeightAsync(skipCount, chainId,
                lastEndHeight);

        if (changePageInfo == null || changePageInfo.IndexerNFTListingChangeList.IsNullOrEmpty())
        {
            _logger.LogInformation(
                "GetNFTListingChangeAsync no data");
            return 0;
        }
        var processChangeOriginList = changePageInfo.IndexerNFTListingChangeList;

        var processChangeList = processChangeOriginList
            .GroupBy(dto => dto.Symbol)
            .Select(group => group.OrderByDescending(dto => dto.BlockHeight).First())
            .ToList();
        _logger.LogInformation(
            "GetNFTListingChangeAsync queryOriginList count: {count} queryList count{count},chainId:{chainId} ",
            processChangeOriginList.Count, processChangeList.Count, chainId);
        
        var blockHeight = await HandleNFTListingChangeAsync(chainId, processChangeList, lastEndHeight);

        maxProcessedBlockHeight = Math.Max(maxProcessedBlockHeight, blockHeight);
        
        return maxProcessedBlockHeight;
    }
    
    private async Task<long> HandleNFTListingChangeAsync(string chainId,
        List<IndexerNFTListingChange> nftListingChangeList, long lastEndHeight)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();
        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        List<string> symbolList = await _distributedCache.GetAsync(cacheKey);
        foreach (var nftListingChange in nftListingChangeList)
        {
            var innerKey = nftListingChange.Symbol + nftListingChange.BlockHeight;
            if (symbolList != null && symbolList.Contains(innerKey))
            {
                _logger.Debug("HandleNFTListingChangeAsync duplicated symbol: {symbol}", nftListingChange.Symbol);
                continue;
            }
            //mark maxProcessedBlockHeight
            blockHeight = Math.Max(blockHeight, nftListingChange.BlockHeight);
            stopwatch.Start();
            await ReceiveListingChangeSignalAsync(nftListingChange);
            stopwatch.Stop();
            _logger.LogInformation(
                "It took {Elapsed} ms to execute HandleNFTListingChangeAsync for symbol ChainId:{chainId} seed symbol: {symbol} blockHeight: {data}.",
                stopwatch.ElapsedMilliseconds, chainId, nftListingChange.Symbol, nftListingChange.BlockHeight);

        }
        if (blockHeight > 0)
        {
            symbolList = nftListingChangeList.Where(obj => obj.BlockHeight == blockHeight)
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

    private async Task ReceiveListingChangeSignalAsync(IndexerNFTListingChange nftListingChange)
    {
        if (nftListingChange == null)
        {
            return;
        }
        await _bus.Publish(new NewIndexEvent<NFTListingChangeEto>
        {
            Data = new NFTListingChangeEto()
            {
                ChainId = nftListingChange.ChainId,
                NftId = IdGenerateHelper.GetNFTInfoId(nftListingChange.ChainId,nftListingChange.Symbol),
                Symbol = nftListingChange.Symbol
            }
        });

        await _bus.Publish<NewIndexEvent<NFTOfferChangeDto>>(new NewIndexEvent<NFTOfferChangeDto>
        {
            Data = new NFTOfferChangeDto
            {
                NftId = IdGenerateHelper.GetNFTInfoId(nftListingChange.ChainId, nftListingChange.Symbol),
                ChainId = nftListingChange.ChainId
            }
        });
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.NftListingChangeNoMainChain;
    }
}