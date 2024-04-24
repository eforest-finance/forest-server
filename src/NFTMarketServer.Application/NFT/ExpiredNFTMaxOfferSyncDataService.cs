using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Caching.Distributed;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Provider;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Options;
using Orleans.Runtime;
using Volo.Abp.Caching;

namespace NFTMarketServer.NFT;

public class ExpiredNftMaxOfferSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IChainAppService _chainAppService;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly ISeedAppService _seedAppService;
    private readonly INESTRepository<NFTInfoIndex, string> _nftInfoIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IOptionsMonitor<ExpiredNFTSyncOptions> _optionsMonitor;
    private readonly IBus _bus;
    private const int HeightExpireSeconds = 0;
    private readonly IDistributedCache<List<string>> _distributedCache;

    public ExpiredNftMaxOfferSyncDataService(ILogger<NftInfoSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        INFTInfoAppService nftInfoAppService,
        IChainAppService chainAppService,
        INFTOfferProvider nftOfferProvider,
        ISeedAppService seedAppService,
        INESTRepository<NFTInfoIndex, string> nftInfoIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IDistributedCache<List<string>> distributedCache,
        IOptionsMonitor<ExpiredNFTSyncOptions> optionsMonitor,
        IBus bus)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _nftInfoAppService = nftInfoAppService;
        _chainAppService = chainAppService;
        _nftOfferProvider = nftOfferProvider;
        _seedAppService = seedAppService;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _optionsMonitor = optionsMonitor;
        _bus = bus;
        _distributedCache = distributedCache;
    }

    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var option = _optionsMonitor.CurrentValue;
        var originList = await _nftOfferProvider.GetNftMaxOfferAsync(chainId, option.Duration);

        long blockHeight = -1;
        if (originList.IsNullOrEmpty())
        {
            _logger.LogInformation("ExpiredNftMaxOfferSync no data, duration: {Duration}", option.Duration);
            return 0;
        }
        
        var list = originList
            .Where(dto => dto.Value != null)
            .GroupBy(dto => dto.Key)
            .Select(group => group.MaxBy(dto => dto.Value?.Prices ?? 0))
            .ToList();
        _logger.LogInformation(
            "ExpiredNftMaxOfferSync queryOriginList count: {count}, queryList count: {count}  from height: {lastEndHeight}, chain id: {chainId}",
            originList.Count, list.Count, lastEndHeight, chainId);

        var cacheKey = GetBusinessType() + chainId + lastEndHeight;
        var nftInfoIdWithTimeList = await _distributedCache.GetAsync(cacheKey);
        var nftInfoIdWithTimeListNew = new List<string>();
        var changeFlag = false;
        foreach (var data in list)
        {
            if (data?.Value == null)
            {
                _logger.Debug("ExpiredNftMaxOfferSync null check");
                continue;
            }
            
            var nftInfoIdWithTime = data.Key + DateTimeHelper.ToUnixTimeMilliseconds(data.Value.ExpireTime);
            nftInfoIdWithTimeListNew.Add(nftInfoIdWithTime);

            if (nftInfoIdWithTimeList != null && nftInfoIdWithTimeList.Contains(nftInfoIdWithTime))
            {
                _logger.Debug($"ExpiredNftMaxOfferSync duplicated nftInfoIdWithTime: {nftInfoIdWithTime}",
                    nftInfoIdWithTime);
                continue;
            }

            changeFlag = true;
            var nftInfoId = data.Key;
            var maxOfferInfo = data.Value;
            _logger.Debug("ExpiredNftMaxOfferSync nftInfoId ={A} offer.symbol={B} maxOfferInfo.id={C}", nftInfoId,
                maxOfferInfo.Symbol, maxOfferInfo?.Id);

            await _bus.Publish<NewIndexEvent<NFTOfferChangeDto>>(new NewIndexEvent<NFTOfferChangeDto>
            {
                Data = new NFTOfferChangeDto
                {
                    NftId = nftInfoId,
                    ChainId = chainId
                }
            });
        }

        if (changeFlag)
        {
            await _distributedCache.SetAsync(cacheKey, nftInfoIdWithTimeListNew,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(HeightExpireSeconds)
                });
        }

        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.ExpiredNftMaxOfferSync;
    }
}