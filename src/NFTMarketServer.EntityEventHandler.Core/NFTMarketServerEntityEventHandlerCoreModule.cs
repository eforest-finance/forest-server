using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace NFTMarketServer.EntityEventHandler.Core
{
    [DependsOn(typeof(AbpAutoMapperModule),
        typeof(NFTMarketServerApplicationModule),
        typeof(NFTMarketServerApplicationContractsModule))]
    public class NFTMarketServerrEntityEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<NFTMarketServerrEntityEventHandlerCoreModule>();
            });
        }
    }
}