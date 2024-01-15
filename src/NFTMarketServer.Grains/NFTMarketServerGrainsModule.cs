using AElf.Client.Service;
using Microsoft.Extensions.DependencyInjection;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.Inscription.Client;
using Volo.Abp.AutoMapper;
using Volo.Abp.Modularity;

namespace NFTMarketServer.Grains;

[DependsOn(typeof(NFTMarketServerApplicationContractsModule),
    typeof(AbpAutoMapperModule))]
public class NFTMarketServerGrainsModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<AbpAutoMapperOptions>(options => { options.AddMaps<NFTMarketServerGrainsModule>(); });
        context.Services.AddSingleton<IBlockchainClientFactory<AElfClient>, AElfClientFactory>();
        context.Services.AddSingleton<IAElfClientProvider, InscriptionAElfClientProvider>();
    }
}