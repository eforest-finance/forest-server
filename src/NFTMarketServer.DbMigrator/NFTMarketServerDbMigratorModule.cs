using NFTMarketServer.MongoDB;
using Volo.Abp.Autofac;
using Volo.Abp.BackgroundJobs;
using Volo.Abp.Modularity;

namespace NFTMarketServer.DbMigrator
{
    [DependsOn(
        typeof(AbpAutofacModule),
        typeof(NFTMarketServerMongoDbModule),
        typeof(NFTMarketServerApplicationContractsModule)
        )]
    public class NFTMarketServerDbMigratorModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpBackgroundJobOptions>(options => options.IsJobExecutionEnabled = false);
        }
    }
}
