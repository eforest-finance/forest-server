using System;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NFTMarketServer.ContractEventHandler.Core;
using NFTMarketServer.ContractEventHandler.Core.Application;
using NFTMarketServer.ContractEventHandler.Core.Worker;
using NFTMarketServer.Grains;
using NFTMarketServer.MongoDB;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc.UI.MultiTenancy;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Caching;
using Volo.Abp.Caching.StackExchangeRedis;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Swashbuckle;
using Volo.Abp.Threading;

namespace NFTMarketServer.ContractEventHandler
{
    [DependsOn(
        typeof(NFTMarketServerGrainsModule),
        typeof(NFTMarketServerContractEventHandlerCoreModule),
        typeof(NFTMarketServerDomainModule),
        typeof(AbpAutofacModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(AbpBackgroundWorkersModule),
        typeof(AbpEventBusRabbitMqModule),
        typeof(AbpBackgroundWorkersQuartzModule),
        typeof(AbpCachingStackExchangeRedisModule),
        typeof(AbpAutoMapperModule),
        typeof(NFTMarketServerMongoDbModule),
        typeof(AbpAspNetCoreSerilogModule),
        typeof(AbpSwashbuckleModule),
        typeof(AbpAspNetCoreMvcUiMultiTenancyModule)
    )]
    public class NFTMarketServerContractEventHandlerModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var configuration = context.Services.GetConfiguration();
            var hostingEnvironment = context.Services.GetHostingEnvironment();
            Configure<ContractSyncOptions>(configuration.GetSection("Sync"));
            context.Services.AddHostedService<NFTMarketHostedService>();
            ConfigureOrleans(context, configuration);
            context.Services.AddSingleton<ISynchronizeTransactionAppService, SynchronizeTransactionAppService>();
            context.Services.AddHttpClient();
            ConfigureCache(configuration);
            ConfigureRedis(context, configuration, hostingEnvironment);
            ConfigureTokenCleanupService();
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            StartOrleans(context.ServiceProvider);
            context.AddBackgroundWorkerAsync<SynchronizeTransactionWorker>();
        }

        public override void OnApplicationShutdown(ApplicationShutdownContext context)
        {
            StopOrleans(context.ServiceProvider);
        }

        private void ConfigureCache(IConfiguration configuration)
        {
            Configure<AbpDistributedCacheOptions>(options => { options.KeyPrefix = "NFTMarketServer:"; });
        }

        private static void ConfigureOrleans(ServiceConfigurationContext context, IConfiguration configuration)
        {
            context.Services.AddSingleton(o =>
            {
                return new ClientBuilder()
                    .ConfigureDefaults()
                    .UseMongoDBClient(configuration["Orleans:MongoDBClient"])
                    .UseMongoDBClustering(options =>
                    {
                        options.DatabaseName = configuration["Orleans:DataBase"];
                        options.Strategy = MongoDBMembershipStrategy.SingleDocument;
                    })
                    .Configure<ClusterOptions>(options =>
                    {
                        options.ClusterId = configuration["Orleans:ClusterId"];
                        options.ServiceId = configuration["Orleans:ServiceId"];
                    })
                    .ConfigureApplicationParts(parts =>
                        parts.AddApplicationPart(typeof(NFTMarketServerGrainsModule).Assembly).WithReferences())
                    .ConfigureLogging(builder => builder.AddProvider(o.GetService<ILoggerProvider>()))
                    .Build();
            });
        }

        private void ConfigureRedis(
            ServiceConfigurationContext context,
            IConfiguration configuration,
            IWebHostEnvironment hostingEnvironment)
        {
            if (!hostingEnvironment.IsDevelopment())
            {
                var redis = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
                context.Services
                    .AddDataProtection()
                    .PersistKeysToStackExchangeRedis(redis, "NFTMarketServer-Protection-Keys");
            }
        }

        private static void StartOrleans(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<IClusterClient>();
            AsyncHelper.RunSync(async () => await client.Connect());
        }

        private static void StopOrleans(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<IClusterClient>();
            AsyncHelper.RunSync(client.Close);
        }

        //Disable TokenCleanupService
        private void ConfigureTokenCleanupService()
        {
            Configure<TokenCleanupOptions>(x => x.IsCleanupEnabled = false);
        }
    }
}