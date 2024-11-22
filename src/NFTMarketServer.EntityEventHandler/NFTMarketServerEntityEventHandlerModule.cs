using AElf.ExceptionHandler.ABP;
using AElf.Indexing.Elasticsearch.Options;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NFTMarketServer.CoinGeckoApi;
using NFTMarketServer.EntityEventHandler;
using NFTMarketServer.EntityEventHandler.Core;
using NFTMarketServer.Grains;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.MongoDB;
using NFTMarketServer.RabbitMq;
using NFTMarketServer.Worker;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace NFTMarketServer;

[DependsOn(typeof(AbpAutofacModule),
    typeof(NFTMarketServerMongoDbModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(NFTMarketServerrEntityEventHandlerCoreModule),
    typeof(NFTMarketServerCoinGeckoApiModule),
    typeof(AbpAspNetCoreSerilogModule),
    typeof(NFTMarketServerWorkerModule),
    typeof(AOPExceptionModule),
    typeof(AbpEventBusRabbitMqModule))]
public class NFTMarketServerEntityEventHandlerModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        ConfigureTokenCleanupService();
        var configuration = context.Services.GetConfiguration();
        
        Configure<WorkerOptions>(configuration);
        Configure<SynchronizeSeedJobOptions>(configuration.GetSection("SynchronizeSeed"));
        Configure<CollectionTradeInfoOptions>(configuration.GetSection("CollectionTradeInfo"));

        context.Services.AddHostedService<NFTMarketServerHostedService>();
        /*context.Services.AddSingleton<IClusterClient>(o =>
        {
            return new ClientBuilder()
                .ConfigureDefaults()
                .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                .UseMongoDBClustering(options =>
                {
                    options.DatabaseName = configuration["Orleans:DataBase"];;
                    options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                })
                .Configure<ClusterOptions>(options =>
                {
                    options.ClusterId = configuration["Orleans:ClusterId"];
                    options.ServiceId = configuration["Orleans:ServiceId"];
                })
                .ConfigureApplicationParts(parts =>
                    parts.AddApplicationPart(typeof(NFTMarketServerGrainsModule).Assembly).WithReferences())
                //.AddSimpleMessageStreamProvider(AElfIndexerApplicationConsts.MessageStreamName)
                .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                .Build();
        });*/
        ConfigureEsIndexCreation();
        ConfigureMassTransit(context);
        ConfigureGraphQl(context, configuration);
    }
    /*public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(async ()=> await client.Connect());
    }*/

    /*public override void OnApplicationShutdown(ApplicationShutdownContext context)
    {
        var client = context.ServiceProvider.GetRequiredService<IClusterClient>();
        AsyncHelper.RunSync(client.Close);
    }*/

    //Create the ElasticSearch Index based on Domain Entity
    private void ConfigureEsIndexCreation()
    {
        Configure<IndexCreateOption>(x => { x.AddModule(typeof(NFTMarketServerDomainModule)); });
    }
    
    //Disable TokenCleanupService
    private void ConfigureTokenCleanupService()
    {
        Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
    }

    private void ConfigureMassTransit(ServiceConfigurationContext context)
    {
        context.Services.AddMassTransit(x =>
        {
            var configuration = context.Services.GetConfiguration();
            x.UsingRabbitMq((ctx, cfg) =>
            {
                var rabbitMqConfig = configuration.GetSection("MassTransit:RabbitMQ").Get<RabbitMqOptions>();
                cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.Port, "/", h =>
                {
                    h.Username(rabbitMqConfig.UserName);
                    h.Password(rabbitMqConfig.Password);
                });
            });
    
        });
    }
    
    private void ConfigureGraphQl(ServiceConfigurationContext context,
        IConfiguration configuration)
    {
        context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
            new NewtonsoftJsonSerializer()));
        context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
    }

}