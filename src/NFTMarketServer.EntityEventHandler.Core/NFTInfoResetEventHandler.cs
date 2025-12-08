using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Etos;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTInfoResetEventHandler : IDistributedEventHandler<NFTInfoResetEto>, ISingletonDependency
{
    private const int ExpireSeconds = 10;
    private readonly ILogger<NFTInfoResetEventHandler> _logger;
    private readonly IDistributedCache<string> _distributedCacheForHeight;
    private readonly INFTInfoAppService _nftInfoAppService;

    public NFTInfoResetEventHandler(ILogger<NFTInfoResetEventHandler> logger,
        IDistributedCache<string> distributedCacheForHeight, INFTInfoAppService nftInfoAppService)
    {
        _logger = logger;
        _distributedCacheForHeight = distributedCacheForHeight;
        _nftInfoAppService = nftInfoAppService;
    }

    public async Task HandleEventAsync(NFTInfoResetEto etoData)
    {
        _logger.LogInformation("NFTInfoResetEventHandler receive: {Data}", JsonConvert.SerializeObject(etoData));
        if (etoData == null || etoData.NFTInfoId.IsNullOrEmpty()) return;

        var expireFlag = await _distributedCacheForHeight.GetAsync(etoData.NFTInfoId);
        if (!expireFlag.IsNullOrEmpty())
        {
            return;
        }

        await _distributedCacheForHeight.SetAsync(etoData.NFTInfoId,
            etoData.NFTInfoId, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(ExpireSeconds)
            });
        
        if (etoData.NFTType == NFTType.NFT)
        {
            await _nftInfoAppService.AddOrUpdateNftInfoNewByIdAsync(etoData.NFTInfoId, etoData.ChainId);
        }

    }
}