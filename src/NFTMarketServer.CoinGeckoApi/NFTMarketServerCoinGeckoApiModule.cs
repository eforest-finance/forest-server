using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;

namespace NFTMarketServer.CoinGeckoApi
{
    [DependsOn(typeof(AbpCachingModule))]
    public class NFTMarketServerCoinGeckoApiModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            Configure<CoinGeckoOptions>(configuration.GetSection("CoinGecko"));
        }
    }
}