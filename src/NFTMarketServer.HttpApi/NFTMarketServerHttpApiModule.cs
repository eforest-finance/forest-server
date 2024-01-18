using System;
using AElf.Whitelist;
using Localization.Resources.AbpUi;
using MassTransit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NFTMarketServer.Hubs;
using NFTMarketServer.Localization;
using NFTMarketServer.RabbitMq;
using Volo.Abp.Account;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.AspNetCore.SignalR;
using Volo.Abp.FeatureManagement;
using Volo.Abp.Identity;
using Volo.Abp.Localization;
using Volo.Abp.Modularity;
using Volo.Abp.PermissionManagement.HttpApi;
using Volo.Abp.SettingManagement;
using Volo.Abp.TenantManagement;

namespace NFTMarketServer
{
    [DependsOn(
        typeof(NFTMarketServerApplicationContractsModule),
        typeof(AbpAccountHttpApiModule),
        typeof(AbpIdentityHttpApiModule),
        typeof(AbpPermissionManagementHttpApiModule),
        typeof(AbpTenantManagementHttpApiModule),
        typeof(AbpFeatureManagementHttpApiModule),
        typeof(AbpSettingManagementHttpApiModule),
        typeof(AElfWhitelistHttpApiModule),
        typeof(AbpAspNetCoreMvcModule),
        typeof(AbpSettingManagementHttpApiModule),
        typeof(AbpAspNetCoreSignalRModule)
    )]
    public class NFTMarketServerHttpApiModule : AbpModule
    {
        public override void PreConfigureServices(ServiceConfigurationContext context)
        {
            PreConfigure<IMvcBuilder>(mvcBuilder =>
            {
                mvcBuilder.AddApplicationPartIfNotExists(typeof(NFTMarketServerHttpApiModule).Assembly);
            });
        }

        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            ConfigureLocalization();
            ConfigureMassTransit(context);
        }

        private void ConfigureLocalization()
        {
            Configure<AbpLocalizationOptions>(options =>
            {
                options.Resources
                    .Get<NFTMarketServerResource>()
                    .AddBaseTypes(
                        typeof(AbpUiResource)
                    );
            });
        }
        
        private void ConfigureMassTransit(ServiceConfigurationContext context)
        {
            context.Services.AddMassTransit(x =>
            {
                var configuration = context.Services.GetConfiguration();
                x.AddConsumer<NewAuctionInfoHandler>();
                x.AddConsumer<NewBidInfoHandler>();
                x.AddConsumer<NewOfferChangeHandler>();
                x.AddConsumer<NFTListingChangeHandler>();
                x.UsingRabbitMq((ctx, cfg) =>
                {
                    var rabbitMqConfig = configuration.GetSection("MassTransit:RabbitMQ").Get<RabbitMqOptions>();
                    cfg.Host(rabbitMqConfig.Host, rabbitMqConfig.Port, "/", h =>
                    {
                        h.Username(rabbitMqConfig.UserName);
                        h.Password(rabbitMqConfig.Password);
                    });

                    var clientQueueName = rabbitMqConfig.ClientQueueName;
                    var machineName = Environment.MachineName;
                    if (!machineName.IsNullOrEmpty())
                    {
                        clientQueueName = clientQueueName + machineName;
                    }

                    cfg.ReceiveEndpoint(clientQueueName, e =>
                    {
                        e.ConfigureConsumer<NewAuctionInfoHandler>(ctx);
                        e.ConfigureConsumer<NewBidInfoHandler>(ctx);
                        e.ConfigureConsumer<NewOfferChangeHandler>(ctx);
                        e.ConfigureConsumer<NFTListingChangeHandler>(ctx);
                    });
                });
    
            });
        }
    }
}