using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Volo.Abp.Caching;

namespace NFTMarketServer.NFT;

public class NFTOfferChangeScheduleService : ScheduleSyncDataService
{
    private const string OfferChangeCachePrefix = "OfferChangeHeight:";
    private const int HeightExpireMinutes = 5;
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly IChainAppService _chainAppService;
    private readonly IBus _bus;
    private readonly IDistributedCache<List<string>> _distributedCache;

    public NFTOfferChangeScheduleService(ILogger<NFTOfferChangeScheduleService> logger,
        IGraphQLProvider graphQlProvider,
        IChainAppService chainAppService,
        INFTOfferProvider nftOfferProvider,
        IBus bus,
        IDistributedCache<List<string>> distributedCache)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _chainAppService = chainAppService;
        _nftOfferProvider = nftOfferProvider;
        _bus = bus;
        _distributedCache = distributedCache;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var queryOriginList = await _nftOfferProvider.GetNFTOfferChangeAsync(chainId, lastEndHeight);
        
        if (CollectionUtilities.IsNullOrEmpty(queryOriginList))
        {
            _logger.LogInformation(
                "GetNFTOfferChangeAsync no data");
            return 0;
        }
        
        var queryList = queryOriginList
            .GroupBy(dto => dto.NftId)
            .Select(group => group.OrderByDescending(dto => dto.BlockHeight).First())
            .ToList();

        _logger.LogInformation(
            "GetNFTOfferChangeAsync queryOriginList count: {count}, queryList count: {count}  from height: {lastEndHeight}, chain id: {chainId}",
            queryOriginList.Count, queryList.Count, lastEndHeight, chainId);
        
        long blockHeight = -1;

        var cacheKey = OfferChangeCachePrefix + chainId + lastEndHeight;
        List<string> nftIdList = await _distributedCache.GetAsync(cacheKey);
        foreach (var queryDto in queryList)
        {
            var innerKey = queryDto.NftId + queryDto.BlockHeight;
            if (nftIdList != null && nftIdList.Contains(innerKey))
            {
                _logger.LogInformation("GetNFTOfferChangeAsync duplicated nftId: {nftId} blockHeight:{B} lastEndHeight:{C}", queryDto.NftId,
                    queryDto.BlockHeight, lastEndHeight);
                continue;
            }
            
            blockHeight = Math.Max(blockHeight, queryDto.BlockHeight);
            await _bus.Publish<NewIndexEvent<NFTOfferChangeDto>>(new NewIndexEvent<NFTOfferChangeDto>
            {
                Data = queryDto
            });
           
            _logger.LogInformation("GetNFTOfferChangeAsync publish nftId: {nftId} blockHeight:{B} lastEndHeight:{C}", queryDto.NftId,
                queryDto.BlockHeight, lastEndHeight);
        }

        if (blockHeight > 0)
        {
            nftIdList = queryList.ToList().Where(obj => obj.BlockHeight == blockHeight)
                .Select(obj => obj.NftId + obj.BlockHeight)
                .ToList();
            await _distributedCache.SetAsync(cacheKey, nftIdList,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(HeightExpireMinutes)
                });
        }
        
        _logger.LogInformation("GetNFTOfferChangeAsync latest block height: {blockHeight}", blockHeight);
        return blockHeight;
    }
    
    public override async Task<List<string>> GetChainIdsAsync()
    {
        var chainId = await _chainAppService.GetChainIdAsync(1);
        return new List<string> { chainId };
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.NftOfferSync;
    }
}