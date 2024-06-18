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

public class NFTMessageActivityEventHandler : IDistributedEventHandler<NFTMessageActivityEto>, ISingletonDependency
{
    private const int ExpireSeconds = 10;
    private readonly ILogger<NFTMessageActivityEventHandler> _logger;
    private readonly IDistributedCache<string> _distributedCacheForHeight;
    private readonly INFTInfoAppService _nftInfoAppService;

    public NFTMessageActivityEventHandler(ILogger<NFTMessageActivityEventHandler> logger,
        IDistributedCache<string> distributedCacheForHeight, INFTInfoAppService nftInfoAppService)
    {
        _logger = logger;
        _distributedCacheForHeight = distributedCacheForHeight;
        _nftInfoAppService = nftInfoAppService;
    }

    public async Task HandleEventAsync(NFTMessageActivityEto etoData)
    {
        _logger.LogInformation("NFTMessageActivityEventHandler receive: {Data}", JsonConvert.SerializeObject(etoData));
        if (etoData == null || etoData.NFTMessageActivityDto == null ||
            etoData.NFTMessageActivityDto.Id.IsNullOrEmpty()) return;

        var dto = etoData.NFTMessageActivityDto;
        var expireFlag = await _distributedCacheForHeight.GetAsync(dto.Id);
        if (!expireFlag.IsNullOrEmpty())
        {
            return;
        }

        await _distributedCacheForHeight.SetAsync(dto.Id,
            dto.Id, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(ExpireSeconds)
            });
        
        //todo

    }
}