using Microsoft.Extensions.DependencyInjection;
using NFTMarketServer.Grains;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.Inscription.Client;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.Modularity;

namespace NFTMarketServer.Silo;

[DependsOn(typeof(AbpAutofacModule),
    typeof(NFTMarketServerGrainsModule),
    typeof(AbpAspNetCoreSerilogModule)
)]
public class NFTMarketServerOrleansSiloModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddHostedService<NFTMarketServerHostedService>();
        var configuration = context.Services.GetConfiguration();
        Configure<SynchronizeTransactionJobOptions>(configuration.GetSection("Synchronize"));
        Configure<SynchronizeSeedJobOptions>(configuration.GetSection("SynchronizeSeed"));
        Configure<ChainOptions>(configuration.GetSection("Chains"));
        Configure<InscriptionChainOptions>(configuration.GetSection("InscriptionChains"));
    }
}