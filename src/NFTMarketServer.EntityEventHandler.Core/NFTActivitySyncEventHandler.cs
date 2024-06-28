using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Provider;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTActivitySyncEventHandler : IDistributedEventHandler<NFTActivitySyncEto>, ISingletonDependency
{
    private const int ExpireSeconds = 10;
    private readonly ILogger<NFTActivitySyncEventHandler> _logger;
    private readonly IDistributedCache<string> _distributedCacheForHeight;
    private readonly INFTActivityProvider _nftActivityProvider;

    public NFTActivitySyncEventHandler(ILogger<NFTActivitySyncEventHandler> logger,
        IDistributedCache<string> distributedCacheForHeight,
        INFTActivityProvider nftActivityProvider)
    {
        _logger = logger;
        _distributedCacheForHeight = distributedCacheForHeight;
        _nftActivityProvider = nftActivityProvider;
    }

    public async Task HandleEventAsync(NFTActivitySyncEto etoData)
    {
        _logger.LogInformation("NFTActivitySyncEventHandler receive: {Data}", JsonConvert.SerializeObject(etoData));
        if (etoData?.NFTActivitySyncDto == null ||
            etoData.NFTActivitySyncDto.Id.IsNullOrEmpty()) return;

        var dto = etoData.NFTActivitySyncDto;
        var key = dto.Id + dto.Type;
        var expireFlag = await _distributedCacheForHeight.GetAsync(key);
        if (!expireFlag.IsNullOrEmpty())
        {
            return;
        }

        await _distributedCacheForHeight.SetAsync(key,
            dto.Id, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(ExpireSeconds)
            });
        
        await _nftActivityProvider.SaveOrUpdateNFTActivityInfoAsync(etoData.NFTActivitySyncDto);
    }
}