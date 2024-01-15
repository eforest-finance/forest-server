using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace NFTMarketServer.ContractEventHandler.Core
{
    [DependsOn(
        typeof(AbpAutoMapperModule)
    )]
    public class NFTMarketServerContractEventHandlerCoreModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<NFTMarketServerContractEventHandlerCoreModule>();
            });
        }
    }
}