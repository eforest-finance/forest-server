using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Chain;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace NFTMarketServer.Worker;

public class TreePointsRecordsSyncWorker : NFTMarketServerWorkBase
{
    protected override BusinessQueryChainType BusinessType => BusinessQueryChainType.TreePointsRecordsSync;
    
    public TreePointsRecordsSyncWorker(ILogger<ScheduleSyncDataContext> logger, AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory, IScheduleSyncDataContext scheduleSyncDataContext, IOptionsMonitor<WorkerOptions> optionsMonitor) : base(logger, timer, serviceScopeFactory, scheduleSyncDataContext, optionsMonitor)
    {
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        await _scheduleSyncDataContext.DealAsync(BusinessQueryChainType.TreePointsRecordsSync, GetResetBlockHeightFlag(), GetResetBlockHeight());
    }
}