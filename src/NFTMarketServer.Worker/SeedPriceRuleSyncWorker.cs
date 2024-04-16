using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Chain;
using NFTMarketServer.Seed;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace NFTMarketServer.Worker;

public class SeedPriceRuleSyncWorker : NFTMarketServerWorkBase
{
    private readonly ISeedPriceAppService _seedPriceAppService;

    public SeedPriceRuleSyncWorker(ILogger<ScheduleSyncDataContext> logger, AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
                                   IScheduleSyncDataContext scheduleSyncDataContext, IOptionsMonitor<WorkerOptions> optionsMonitor, ISeedPriceAppService seedPriceAppService) :
        base(logger, timer, serviceScopeFactory,
            scheduleSyncDataContext, optionsMonitor)
    {
        _seedPriceAppService = seedPriceAppService;
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _scheduleSyncDataContext.DealAsync(BusinessQueryChainType.RegularPriceRule, GetResetBlockHeightFlag(), GetResetBlockHeight());
        await _scheduleSyncDataContext.DealAsync(BusinessQueryChainType.UniquePriceRule, GetResetBlockHeightFlag(), GetResetBlockHeight());
        await _seedPriceAppService.UpdateUniqueAllNoBurnSeedPriceAsync();
    }

    protected override BusinessQueryChainType BusinessType => BusinessQueryChainType.SeedPriceRule;
}