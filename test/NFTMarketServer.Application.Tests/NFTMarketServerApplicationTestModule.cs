using GraphQL.Client.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.CoinGeckoApi;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Dealer.Handler;
using NFTMarketServer.Dealer.Provider;
using NFTMarketServer.Dealer.Worker;
using NFTMarketServer.EntityEventHandler.Core;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.Order;
using NFTMarketServer.Worker;
using NFTMarkte.Orleans.TestBase;
using NSubstitute;
using Volo.Abp;
using Volo.Abp.AuditLogging;
using Volo.Abp.AuditLogging.MongoDB;
using Volo.Abp.AutoMapper;
using Volo.Abp.BlobStoring;
using Volo.Abp.EventBus;
using Volo.Abp.Identity;
using Volo.Abp.Identity.MongoDB;
using Volo.Abp.Modularity;
using Volo.Abp.MongoDB;

namespace NFTMarketServer
{
    [DependsOn(
        typeof(NFTMarketServerApplicationModule),
        typeof(NFTMarketServerrEntityEventHandlerCoreModule),
        typeof(AbpEventBusModule),
        typeof(NFTMarkteServerOrleansTestBaseModule),
        typeof(NFTMarketServerDomainTestModule)
    )]
    public class NFTMarketServerApplicationTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            base.ConfigureServices(context);
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<NFTMarketServerApplicationModule>(); });
            Configure<AbpAutoMapperOptions>(options => { options.AddMaps<NFTMarketServerDealerModule>(); });
            Configure<AbpAutoMapperOptions>(options =>
            {
                options.AddMaps<NFTMarketServerrEntityEventHandlerCoreModule>();
            });
            context.Services.AddSingleton(new Mock<IMongoDbContextProvider<IAuditLoggingMongoDbContext>>().Object);
            context.Services.AddSingleton<IAuditLogRepository, MongoAuditLogRepository>();
            context.Services.AddSingleton<IIdentityUserRepository, MongoIdentityUserRepository>();
            context.Services.AddSingleton<TestEnvironmentProvider>();
            context.Services.AddSingleton<TestEnvironmentProvider>();
            context.Services.AddSingleton<IBlobContainer, MockBlobContainer>();
            context.Services.AddSingleton(Substitute.For<IGraphQLClient>());
            context.Services.AddSingleton(Substitute.For<IGraphQLHelper>());
            
            context.Services.AddSingleton<ContractInvokeHandler>();
            context.Services.AddSingleton<ContractInvokeChangedHandler>();
            context.Services.AddSingleton<IContractProvider, ContractProvider>();
            context.Services.AddSingleton<IAelfClientProvider, AelfClientProvider>();
            context.Services.AddSingleton<IContractInvokerWorker, ContractInvokerWorker>();
            
            context.Services.AddSingleton<IPortkeyClientProvider, MockPortkeyClientProvider>();
            context.Services.AddSingleton<INFTActivityAppService, NFTActivityAppService>();
            context.Services.AddSingleton<INFTActivityProvider, NFTActivityProvider>();

            context.Services.Configure<PortkeyOption>(o =>
            {
                o.Name = "Portkey";
                o.PrivateKey = "e815acba8fcf085a0b4141060c13b8017a08da37f2eb1d6a5416adbb621560ef";
                o.PublicKey = "043a2361270fb1bdd220a2fa0f187cc6f85079043a56fb6a968dfad7d7032b07b01213e80ecd4fb41f1500f94698b1117bc9f3335bde5efbb1330271afc6e85e92";
            });
            context.Services.Configure<CoinGeckoOptions>(o => { o.CoinIdMapping["ELF"] = "aelf"; });
        }

        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
        }
    }
}