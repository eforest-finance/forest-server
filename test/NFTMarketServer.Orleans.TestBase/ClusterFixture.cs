using AutoMapper;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using NFTMarketServer.Grains;
using Orleans.Hosting;
using Orleans.TestingHost;
using Volo.Abp.AutoMapper;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Reflection;

namespace NFTMarketServer.Orleans.TestBase;

public class ClusterFixture : IDisposable, ISingletonDependency
{
    public ClusterFixture()
    {
        var builder = new TestClusterBuilder();
        builder.AddSiloBuilderConfigurator<TestSiloConfigurations>();
        // builder.AddClientBuilderConfigurator<TestClientBuilderConfigurator>();
        Cluster = builder.Build();
        Cluster.Deploy();
    }

    public void Dispose()
    {
        Cluster.StopAllSilos();
    }

    public TestCluster Cluster { get; private set; }


    private class TestSiloConfigurations : ISiloBuilderConfigurator
    {
        public void Configure(ISiloHostBuilder hostBuilder)
        {
            hostBuilder.ConfigureServices(services =>
                {
                    services.AddMemoryCache();
                    services.AddDistributedMemoryCache();
                    services.AddAutoMapper(typeof(NFTMarketServerGrainsModule).Assembly);
                    services.AddSingleton(typeof(IDistributedCache), typeof(MemoryDistributedCache));
                    services.AddSingleton(typeof(IDistributedCache<,>), typeof(DistributedCache<,>));
                    services.Configure<AbpDistributedCacheOptions>(cacheOptions =>
                    {
                        cacheOptions.GlobalCacheEntryOptions.SlidingExpiration = TimeSpan.FromMinutes(20);
                    });
                    services.OnExposing(onServiceExposingContext =>
                    {
                        //Register types for IObjectMapper<TSource, TDestination> if implements
                        onServiceExposingContext.ExposedTypes.AddRange(
                            ReflectionHelper.GetImplementedGenericTypes(
                                onServiceExposingContext.ImplementationType,
                                typeof(IObjectMapper<,>)
                            )
                        );
                    });
                    services.AddTransient(typeof(IObjectMapper<>), typeof(DefaultObjectMapper<>));
                    services.AddTransient(typeof(IObjectMapper), typeof(DefaultObjectMapper));
                    services.AddTransient(typeof(IAutoObjectMappingProvider),
                        typeof(AutoMapperAutoObjectMappingProvider));
                    services.AddTransient(sp => new MapperAccessor()
                    {
                        Mapper = sp.GetRequiredService<IMapper>()
                    });
                    services.AddTransient<IMapperAccessor>(provider => provider.GetRequiredService<MapperAccessor>());
                })
                .AddMemoryGrainStorage("PubSubStore")
                .AddMemoryGrainStorageAsDefault();
        }
    }

    public class MapperAccessor : IMapperAccessor
    {
        public IMapper Mapper { get; set; }
    }
}