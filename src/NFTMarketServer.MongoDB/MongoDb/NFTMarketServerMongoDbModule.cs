using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.AuditLogging.MongoDB;
using Volo.Abp.FeatureManagement.MongoDB;
using Volo.Abp.Identity.MongoDB;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.MongoDB;
using Volo.Abp.PermissionManagement.MongoDB;
using Volo.Abp.SettingManagement.MongoDB;
using Volo.Abp.TenantManagement.MongoDB;
using Volo.Abp.Uow;

namespace NFTMarketServer.MongoDB;

[DependsOn(
    typeof(NFTMarketServerDomainModule),
    typeof(AbpPermissionManagementMongoDbModule),
    typeof(AbpSettingManagementMongoDbModule),
    typeof(AbpIdentityMongoDbModule),
    typeof(AbpOpenIddictMongoDbModule),
    typeof(AbpAuditLoggingMongoDbModule),
    typeof(AbpTenantManagementMongoDbModule),
    typeof(AbpFeatureManagementMongoDbModule)
    )]
public class NFTMarketServerMongoDbModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddMongoDbContext<NFTMarketServerMongoDbContext>(options =>
        {
            options.AddDefaultRepositories();
        });

        Configure<AbpUnitOfWorkDefaultOptions>(options =>
        {
            options.TransactionBehavior = UnitOfWorkTransactionBehavior.Disabled;
        });
    }
}
