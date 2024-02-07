using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hangfire;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Dealer.Options;
using NFTMarketServer.Dealer.Worker;

namespace NFTMarketServer.Dealer;

public class InitJobsService : BackgroundService
{
    private readonly IRecurringJobManager _recurringJobs;
    private readonly IOptionsMonitor<WorkerOption> _workerOptionsMonitor;
    private readonly ILogger<InitJobsService> _logger;

    public InitJobsService(IRecurringJobManager recurringJobs, 
        IOptionsMonitor<WorkerOption> workerOptionsMonitor, ILogger<InitJobsService> logger)
    {
        _recurringJobs = recurringJobs;
        _logger = logger;
        _workerOptionsMonitor = workerOptionsMonitor;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            _recurringJobs.AddOrUpdate<IContractInvokerWorker>("IContractInvokerWorker",
                x => x.Invoke(), _workerOptionsMonitor.CurrentValue?.Workers?.GetValueOrDefault("IContractInvokerWorker")?.Cron ?? WorkerOption.DefaultCron);
            _recurringJobs.AddOrUpdate<INFTDropFinishWorker>("INFTDropFinishWorker",
                x => x.CheckExpireDrop(), _workerOptionsMonitor.CurrentValue?.Workers?.GetValueOrDefault("INFTDropFinishWorker")?.Cron ?? WorkerOption.DefaultCron);
        }
        catch (Exception e)
        {
            _logger.LogError("An exception occurred while creating recurring jobs.", e);
        }

        return Task.CompletedTask;
    }
}