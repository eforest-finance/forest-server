using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.Users.Provider;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTBalanceEventHandler : IDistributedEventHandler<UserBalanceEto>, ISingletonDependency
{
    private const int ExpireSeconds = 10;
    private readonly ILogger<NFTBalanceEventHandler> _logger;
    private readonly IDistributedCache<string> _distributedCacheForHeight;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IUserBalanceProvider _userBalanceProvider;

    public NFTBalanceEventHandler(ILogger<NFTBalanceEventHandler> logger,
        IDistributedCache<string> distributedCacheForHeight,
        INFTInfoAppService nftInfoAppService,
        IUserBalanceProvider userBalanceProvider)
    {
        _logger = logger;
        _distributedCacheForHeight = distributedCacheForHeight;
        _nftInfoAppService = nftInfoAppService;
        _userBalanceProvider = userBalanceProvider;
    }

    public async Task HandleEventAsync(UserBalanceEto etoData)
    {
        _logger.LogInformation("NFTBalanceEventHandler receive: {Data}", JsonConvert.SerializeObject(etoData));
        if (etoData?.UserBalanceDto == null ||
            etoData.UserBalanceDto.Id.IsNullOrEmpty()) return;

        var dto = etoData.UserBalanceDto;
        var expireFlag = await _distributedCacheForHeight.GetAsync(dto.Id);
        if (!expireFlag.IsNullOrEmpty())
        {
            _logger.LogInformation("NFTBalanceEventHandler expireFlag: {expireFlag},Id:{Id}",expireFlag, etoData.UserBalanceDto.Id);
            return;
        }

        await _distributedCacheForHeight.SetAsync(dto.Id,
            dto.Id, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(ExpireSeconds)
            });
        
        await _userBalanceProvider.SaveOrUpdateUserBalanceAsync(etoData.UserBalanceDto);
    }
    
}