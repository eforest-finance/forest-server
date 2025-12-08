using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.NFT.Etos;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTResetFlagEventHandler : IDistributedEventHandler<NFTResetFlagEto>, ISingletonDependency
{
    private readonly ILogger<NFTResetFlagEventHandler> _logger;
    private readonly IDistributedCache<string> _distributedCacheForHeight;

    public NFTResetFlagEventHandler(ILogger<NFTResetFlagEventHandler> logger,
        IDistributedCache<string> distributedCacheForHeight)
    {
        _logger = logger;
        _distributedCacheForHeight = distributedCacheForHeight;
    }

    public async Task HandleEventAsync(NFTResetFlagEto etoData)
    {
        _logger.LogInformation("NFTResetFlagEventHandler receive: {Data}", JsonConvert.SerializeObject(etoData));
        if (etoData == null || etoData.FlagDesc.IsNullOrEmpty()) return;
        await _distributedCacheForHeight.SetAsync(etoData.FlagDesc,
            etoData.Minutes.ToString(), new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddMinutes(etoData.Minutes)
            });
    }
}