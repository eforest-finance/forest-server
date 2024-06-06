using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface IRarityProvider
{
    public Task<bool> CheckAddressIsInWhiteListAsync(string address);
}

public class RarityProvider : IRarityProvider, ISingletonDependency
{
    private readonly ILogger<RarityProvider> _logger;
    
    private readonly IOptionsMonitor<RarityShowWhiteOptions> _rarityShowWhiteOptionsMonitor;
    
    public RarityProvider(IOptionsMonitor<RarityShowWhiteOptions> rarityShowWhiteOptionsMonitor,
        ILogger<RarityProvider> logger)
    {
        _rarityShowWhiteOptionsMonitor = rarityShowWhiteOptionsMonitor;
        _logger = logger;
    }

    public async Task<bool> CheckAddressIsInWhiteListAsync(string address)
    {
        return true;
        var whiteList = _rarityShowWhiteOptionsMonitor.CurrentValue.RarityShowWhiteList;
        if (address.IsNullOrEmpty())
        {
            return false;
        }

        if (whiteList.IsNullOrEmpty())
        {
            return false;
        }

        return whiteList.Contains(address);
    }
}