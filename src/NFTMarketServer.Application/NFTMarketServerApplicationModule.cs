using System;
using System.Linq;
using AElf.Whitelist;
using Elasticsearch.Net;
using Medallion.Threading;
using Medallion.Threading.Redis;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Nest;
using NFTMarketServer.Bid;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Dealer.ContractInvoker.Inscription;
using NFTMarketServer.Grains;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Inscription;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.OwnerShip.Verify;
using NFTMarketServer.Provider;
using NFTMarketServer.Redis;
using NFTMarketServer.Seed;
using StackExchange.Redis;
using Volo.Abp.Account;
using Volo.Abp.AutoMapper;
using Volo.Abp.EventBus.RabbitMq;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;
using IConfiguration = Microsoft.Extensions.Configuration.IConfiguration;

namespace NFTMarketServer
{
    [DependsOn(
        typeof(NFTMarketServerDomainModule),
        typeof(AbpAccountApplicationModule),
        typeof(NFTMarketServerApplicationContractsModule),
        typeof(AbpIdentityApplicationModule),
        typeof(AbpPermissionManagementApplicationModule),
        typeof(AbpTenantManagementApplicationModule),
        typeof(AbpFeatureManagementApplicationModule),
        typeof(AbpSettingManagementApplicationModule),
        typeof(NFTMarketServerGrainsModule),
        typeof(AElfWhitelistApplicationModule),
        typeof(AbpEventBusRabbitMqModule) 
    )]
    public class NFTMarketServerApplicationModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            // PreConfigure<AbpEventBusOptions>(options =>
            // {
            //     options.EnabledErrorHandle = true;
            //     options.UseRetryStrategy(retryStrategyOptions =>
            //     {
            //         retryStrategyOptions.IntervalMillisecond = 1000;
            //         retryStrategyOptions.MaxRetryAttempts = 100;
            //     });
            // });
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<NFTMarketServerApplicationModule>(); });

            var configuration = context.Services.GetConfiguration();
            
            context.Services.AddSingleton<IOwnerShipVerify, EthOwnerShipVerify>();
            context.Services.AddSingleton<INFTListingProvider, NFTListingProvider>();
            context.Services.AddSingleton<INFTListingWhitelistPriceProvider, NFTListingWhitelistPriceProvider>();

            context.Services.AddSingleton<IContractInvoker, AuctionAutoClaimInvoker>();
            context.Services.AddSingleton<IContractInvoker, CreateSeedContractInvoker>();
            context.Services.AddSingleton<IContractInvoker, InscriptionCollectionValidateTokenInfoExistsInvoker>();
            context.Services.AddSingleton<IContractInvoker, InscriptionItemValidateTokenInfoExistsInvoker>();
            context.Services.AddSingleton<IContractInvoker, InscriptionItemCrossChainCreateInvoker>();
            context.Services.AddSingleton<IContractInvoker, InscriptionCollectionCrossChainCreateInvoker>();
            context.Services.AddSingleton<IContractInvoker, InscriptionIssueInvoker>();
            context.Services.AddSingleton<IContractInvoker, NFTDropFinishInvoker>();

            context.Services.AddSingleton<InscriptionItemCrossChainCreateInvoker>();
            context.Services.AddSingleton<InscriptionCollectionCrossChainCreateInvoker>();
            context.Services.AddSingleton<InscriptionIssueInvoker>();


            context.Services.AddTransient<IScheduleSyncDataService, SeedIconScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, AuctionScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, RegularSeedPriceRuleScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, UniqueSeedPriceRuleScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, SeedMainChainCreateScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, NFTListingChangeScheduleService>();

            context.Services.AddTransient<IScheduleSyncDataService, BidScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, NFTCollectionStatisticalDataScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, CollectionExtenstionCurrentInitScheduleService>(); 
            context.Services.AddTransient<IScheduleSyncDataService, NFTCollectionPriceScheduleService>();   
            context.Services.AddTransient<IScheduleSyncDataService, TsmSeedMainChainScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, TsmSeedSideChainScheduleService>();
            //context.Services.AddTransient<IScheduleSyncDataService, NftInfoSyncDataService>();
            context.Services.AddTransient<IScheduleSyncDataService, NftInfoNewSyncDataService>();
            context.Services.AddTransient<IScheduleSyncDataService, NftInfoNewRecentSyncDataService>();
            context.Services.AddTransient<IScheduleSyncDataService, SeedSymbolSyncDataService>();
            context.Services.AddTransient<IScheduleSyncDataService, ExpiredListingNftHandleService>();
            context.Services.AddTransient<IScheduleSyncDataService, ExpiredNftMinPriceSyncDataService>();
            context.Services.AddTransient<IScheduleSyncDataService, ExpiredNftMaxOfferSyncDataService>();
            context.Services.AddTransient<IScheduleSyncDataService, NFTOfferChangeScheduleService>();

            context.Services.AddTransient<IScheduleSyncDataService, InscriptionCrossChainScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, NFTActivityMessageScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, NFTActivitySyncScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, NFTActivityTransferSyncScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, UserBalanceSyncScheduleService>();
            context.Services.AddTransient<IScheduleSyncDataService, TreePointsRecordsSyncScheduleService>();

            Configure<GraphQLOptions>(configuration.GetSection("GraphQL"));
            Configure<AwsS3Option>(configuration.GetSection("AwsS3"));
            Configure<PortkeyOption>(configuration.GetSection("Portkey"));
            Configure<ExpiredNFTSyncOptions>(configuration.GetSection("ExpiredNftSync"));
            Configure<ChainOptions>(configuration.GetSection("Chains"));
            Configure<ChainOption>(configuration.GetSection("ChainOption"));
            Configure<SynchronizeTransactionJobOptions>(configuration.GetSection("Synchronize"));
            Configure<OpenAiOptions>(configuration.GetSection("OpenAi"));
            Configure<StatisticsUserListRecordOptions>(configuration.GetSection("StatisticsUserListRecordOptions"));
            Configure<FuzzySearchOptions>(configuration.GetSection("FuzzySearchOptions"));
            Configure<PlatformNFTOptions>(configuration.GetSection("PlatformNFT"));
            Configure<TreeGameOptions>(configuration.GetSection("TreeGame"));

            ConfigureTokenBucketService(context, configuration);
            ConfigureDistributedLocking(context, configuration);
            ConfigureElasticsearch(context, configuration);
        }
        
        private static void ConfigureTokenBucketService(
            ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            context.Services.AddSingleton<IOpenAiRedisTokenBucket>(sp =>
            {
                var connection = ConnectionMultiplexer.Connect(configuration["Redis:Configuration"]);
                var database = connection.GetDatabase();
                return new OpenAiRedisTokenBucket(database, "openAiTokenBucket",
                    configuration.GetSection("OpenAi").GetSection("ApiKeyList").Get<string[]>().Length-1);
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
        
        private static void ConfigureElasticsearch(
            ServiceConfigurationContext context,
            IConfiguration configuration)
        {
            context.Services.AddSingleton<IElasticClient>(sp =>
            {
                var uris = configuration.GetSection("ElasticUris:Uris").Get<string[]>();
                if (uris == null || uris.Length == 0)
                {
                    throw new ArgumentNullException("ElasticUris:Uris", "Elasticsearch URIs cannot be null or empty.");
                }

                var settings = new ConnectionSettings(new StaticConnectionPool(uris.Select(uri => new Uri(uri)).ToArray()));

                return new ElasticClient(settings);
            });
    
        } 
    }
}