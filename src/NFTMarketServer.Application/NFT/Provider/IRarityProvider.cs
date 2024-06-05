using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface IRarityProvider
{
    public Task<bool> CheckAddressIsInWhiteListAsync(string address);
}

public class RarityProvider : IRarityProvider, ISingletonDependency
{
    private readonly IOptionsMonitor<RarityShowWhiteOptions> _rarityShowWhiteOptionsMonitor;
    
    public RarityProvider(IOptionsMonitor<RarityShowWhiteOptions> rarityShowWhiteOptionsMonitor)
    {
        _rarityShowWhiteOptionsMonitor = rarityShowWhiteOptionsMonitor;
    }

    public async Task<bool> CheckAddressIsInWhiteListAsync(string address)
    {
        var whiteList = _rarityShowWhiteOptionsMonitor.CurrentValue.RarityShowWhiteList;
        if (address.IsNullOrEmpty() || whiteList.IsNullOrEmpty())
        {
            return false;
        }

        return whiteList.Contains(address);
    }
}