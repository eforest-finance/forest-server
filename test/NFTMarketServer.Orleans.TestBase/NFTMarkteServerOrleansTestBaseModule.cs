using Microsoft.Extensions.DependencyInjection;
using NFTMarketServer;
using NFTMarketServer.Orleans.TestBase;
using Orleans;
using Volo.Abp;
using Volo.Abp.Authorization;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.Modularity;
using Volo.Abp.ObjectMapping;

namespace NFTMarkte.Orleans.TestBase;

[DependsOn(
    typeof(AbpAutofacModule),
    typeof(AbpTestBaseModule),
    typeof(AbpAuthorizationModule),
    typeof(NFTMarketServerDomainModule),
    typeof(AbpCachingModule),
    typeof(AbpAutoMapperModule),
    typeof(AbpObjectMappingModule)
)]
public class NFTMarkteServerOrleansTestBaseModule : AbpModule
{
    private ClusterFixture _fixture;

    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        if (_fixture == null)
            _fixture = new ClusterFixture();
        context.Services.AddSingleton(_fixture);
        context.Services.AddSingleton<IClusterClient>(sp => _fixture.Cluster.Client);
    }
}