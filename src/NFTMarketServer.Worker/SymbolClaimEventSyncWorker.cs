using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Bid;
using NFTMarketServer.Chain;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace NFTMarketServer.Worker;

public class SymbolClaimEventSyncWorker : NFTMarketServerWorkBase
{ 
    private readonly ISymbolAutoClaimService _symbolAutoClaimService;

  
    protected override BusinessQueryChainType BusinessType => BusinessQueryChainType.SeedAutoClaim;

    public SymbolClaimEventSyncWorker(ILogger<ScheduleSyncDataContext> logger, 
                                      AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
                                      IScheduleSyncDataContext scheduleSyncDataContext, IOptionsMonitor<WorkerOptions> optionsMonitor, ISymbolAutoClaimService symbolAutoClaimService) 
        : base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor)
    {
        _symbolAutoClaimService = symbolAutoClaimService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _symbolAutoClaimService.SyncSymbolClaimAsync();
    }
}