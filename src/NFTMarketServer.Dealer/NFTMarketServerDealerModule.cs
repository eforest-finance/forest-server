using System;
using AElf.ExceptionHandler.ABP;
using AElf.Indexing.Elasticsearch.Options;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Mongo;
using Hangfire.Mongo.CosmosDB;
using Hangfire.Mongo.Migration.Strategies;
using Hangfire.Mongo.Migration.Strategies.Backup;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Dealer.Options;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Grains;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using Orleans;
using Orleans.Configuration;
using Orleans.Providers.MongoDB.Configuration;
using StackExchange.Redis;
using SymbolMarketServer.Options;
using Volo.Abp;
using Volo.Abp.AspNetCore.Serilog;
using Volo.Abp.Autofac;
using Volo.Abp.AutoMapper;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.BackgroundWorkers.Quartz;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.Modularity;
using Volo.Abp.OpenIddict.Tokens;
using Volo.Abp.Threading;

namespace NFTMarketServer.Dealer
{
    [DependsOn(
        typeof(NFTMarketServerGrainsModule),
        typeof(NFTMarketServerDomainModule),
        typeof(AbpAutofacModule),
        typeof(AbpBackgroundWorkersModule),
        typeof(AbpEventBusRabbitMqModule),
        typeof(AbpBackgroundWorkersQuartzModule),
        typeof(AbpAutoMapperModule),
        typeof(AbpAspNetCoreSerilogModule)
       // typeof(AOPExceptionModule)
    )]
    public class NFTMarketServerDealerModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<NFTMarketServerApplicationModule>(); });

            ConfigureTokenCleanupService();
            var configuration = context.Services.GetConfiguration();
            Configure<ChainOption>(configuration.GetSection("ChainOption"));
            Configure<WorkerOption>(configuration.GetSection("WorkerSettings"));

            ConfigOrleans(context, configuration);
            ConfigureCache(configuration);
            ConfigureGraphQl(context, configuration);
            ConfigureDistributedLocking(context, configuration);
            ConfigureHangfire(context, configuration);
            
            ConfigureEsIndexCreation();

            context.Services.AddSingleton<IHostedService, NFTMarketHostedService>();
            context.Services.AddSingleton<IHostedService, InitJobsService>();
            
            context.Services.AddSingleton<ContractInvokeProvider>();
            context.Services.AddSingleton<IContractInvokerFactory, ContractInvokerFactory>();
            context.Services.AddSingleton<IGraphQLClientFactory, GraphQLClientFactory>();
            context.Services.AddSingleton<INFTDropInfoProvider, NFTDropInfoProvider>();
            context.Services.AddSingleton<IContractInvoker, NFTDropFinishInvoker>();
            
            
            Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
            Configure<ForestChainOptions>(configuration.GetSection("Forest"));
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var app = context.GetApplicationBuilder();
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                // IsReadOnlyFunc = (DashboardContext context) => true
            });
          //  StartOrleans(context.ServiceProvider);
        }
        
        /*private static void StartOrleans(IServiceProvider serviceProvider)
        {
            var client = serviceProvider.GetRequiredService<IClusterClient>();
            if(!client.IsInitialized) 
            {
                AsyncHelper.RunSync(async () => await client.Connect());
            }
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

        private void ConfigureCache(IConfiguration configuration)
        {
            var cacheOptions = configuration.GetSection("Cache").Get<CacheOptions>();
            var expirationDays = cacheOptions?.ExpirationDays ?? CommonConstant.CacheExpirationDays;

            Configure<AbpDistributedCacheOptions>(options =>
            {
                options.KeyPrefix = "TSM:dealer:";
                options.GlobalCacheEntryOptions = new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(expirationDays)
                };
            });
        }
        

        private void ConfigureHangfire(ServiceConfigurationContext context, IConfiguration configuration)
        {
            /*context.Services.AddHangfire(x =>
            {
                var connectionString = configuration["Hangfire:ConnectionString"];
                x.UseMongoStorage(connectionString, new MongoStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        MigrationStrategy = new MigrateMongoMigrationStrategy(),
                        BackupStrategy = new CollectionMongoBackupStrategy()
                    },
                    // Prefix = "hangfire.mongo",
                    CheckConnection = true,
                    CheckQueuedJobsStrategy = CheckQueuedJobsStrategy.TailNotificationsCollection
                });

                x.UseDashboardMetric(DashboardMetrics.ServerCount)
                    .UseDashboardMetric(DashboardMetrics.RecurringJobCount)
                    .UseDashboardMetric(DashboardMetrics.RetriesCount)
                    .UseDashboardMetric(DashboardMetrics.AwaitingCount)
                    .UseDashboardMetric(DashboardMetrics.EnqueuedAndQueueCount)
                    .UseDashboardMetric(DashboardMetrics.ScheduledCount)
                    .UseDashboardMetric(DashboardMetrics.ProcessingCount)
                    .UseDashboardMetric(DashboardMetrics.SucceededCount)
                    .UseDashboardMetric(DashboardMetrics.FailedCount)
                    .UseDashboardMetric(DashboardMetrics.EnqueuedCountOrNull)
                    .UseDashboardMetric(DashboardMetrics.FailedCountOrNull)
                    .UseDashboardMetric(DashboardMetrics.DeletedCount);
            });
            context.Services.AddHangfireServer();*/
            
            // Add framework services.
            context.Services.AddHangfire(config =>
            {
                var connectionString =  configuration["Hangfire:ConnectionString"];
                var mongoUrlBuilder = new MongoUrlBuilder(connectionString) {DatabaseName = "jobs"};
                var mongoClient = new MongoClient(mongoUrlBuilder.ToMongoUrl());
                var opt = new CosmosStorageOptions
                {
                    MigrationOptions = new MongoMigrationOptions
                    {
                        BackupStrategy = new NoneMongoBackupStrategy(),
                        MigrationStrategy = new DropMongoMigrationStrategy(),
                    }
                };
                //config.UseLogProvider(new FileLogProvider());
                config.UseCosmosStorage(mongoClient, mongoUrlBuilder.DatabaseName, opt);
            });
            
            context.Services.AddHangfireServer(opt =>
            {
                opt.Queues = new[] {"default", "notDefault"};
            });
            
        }


        private static void ConfigureDistributedLocking(
            ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            context.Services.AddSingleton<IDistributedLockProvider>(sp =>
            {
                var connection = ConnectionMultiplexer
                    .Connect(configuration["Redis:Configuration"]);
                return new RedisDistributedSynchronizationProvider(connection.GetDatabase());
            });
        }

        private static void ConfigOrleans(ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            /*context.Services.AddSingleton<IClusterClient>(o =>
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
            });*/
        }

        private static void ConfigureGraphQl(ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            context.Services.AddSingleton(new GraphQLHttpClient(configuration["GraphQL:Configuration"],
                new NewtonsoftJsonSerializer()));
            context.Services.AddScoped<IGraphQLClient>(sp => sp.GetRequiredService<GraphQLHttpClient>());
        }
    }
}