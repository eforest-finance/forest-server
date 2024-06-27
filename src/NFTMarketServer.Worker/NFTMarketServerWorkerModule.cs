using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Modularity;

namespace NFTMarketServer.Worker
{
    [DependsOn(typeof(NFTMarketServerApplicationContractsModule),
        typeof(AbpBackgroundWorkersModule))]
    public class NFTMarketServerWorkerModule : AbpModule
    {
        public override void OnApplicationInitialization(ApplicationInitializationContext context)
        {
            var backgroundWorkerManger = context.ServiceProvider.GetRequiredService<IBackgroundWorkerManager>();
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SymbolBidEventSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SymbolClaimEventSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTCollectionStatisticalDataSyncWorker>());backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<CollectionExtenstionCurrentInitWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTCollectionPriceSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SeedIconSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<TsmSeedSymbolSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SeedPriceRuleSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SeedMainChainCreateSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTInfoSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTInfoNewSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTInfoNewRecentSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<SeedSymbolSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ExpiredListingNftHandleWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ExpiredNFTMinPriceSyncWorker>());

            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<ExpiredNFTMaxOfferSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTOfferSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTListingChangeNoMainChainWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<InscriptionSyncWorker>()); 
			backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTActivityMessageWorker>());
			backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTActivitySyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<NFTActivityTransferSyncWorker>());
            backgroundWorkerManger.AddAsync(context.ServiceProvider.GetService<UserBalanceSyncWorker>());

        }
    }
}