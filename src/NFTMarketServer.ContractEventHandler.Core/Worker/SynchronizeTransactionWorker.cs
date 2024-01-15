using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.ContractEventHandler.Core.Application;
using Volo.Abp.BackgroundWorkers;
using Volo.Abp.Threading;

namespace NFTMarketServer.ContractEventHandler.Core.Worker;

public class SynchronizeTransactionWorker : AsyncPeriodicBackgroundWorkerBase
{
    private readonly ISynchronizeTransactionAppService _synchronizeTransactionAppService;

    public SynchronizeTransactionWorker(AbpAsyncTimer timer, IServiceScopeFactory serviceScopeFactory,
        ISynchronizeTransactionAppService synchronizeTransactionAppService,
        IOptionsMonitor<ContractSyncOptions> contractSyncOptionsMonitor) :
        base(timer, serviceScopeFactory)
    {
        _synchronizeTransactionAppService = synchronizeTransactionAppService;
        Timer.Period = 1000 * contractSyncOptionsMonitor.CurrentValue.Sync;
        contractSyncOptionsMonitor.OnChange((newOptions, _) =>
        {
            Timer.Period = 1000 * contractSyncOptionsMonitor.CurrentValue.Sync;
        });
    }

    protected override async Task DoWorkAsync(PeriodicBackgroundWorkerContext workerContext)
    {
        Logger.LogInformation("Executing sync NFT transaction job");
        var syncTxHashes = await _synchronizeTransactionAppService.SearchUnfinishedSynchronizeTransactionAsync();

        var tasks = new List<Task>();

        foreach (var syncTxHash in syncTxHashes)
        {
            tasks.Add(Task.Run(() => { _synchronizeTransactionAppService.ExecuteJobAsync(syncTxHash); }));
        }

        await Task.WhenAll(tasks);
    }
}