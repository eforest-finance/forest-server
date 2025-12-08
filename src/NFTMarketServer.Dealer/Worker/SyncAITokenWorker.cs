using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Volo.Abp.DependencyInjection;
using NFTMarketServer.Synchronize;

namespace NFTMarketServer.Dealer.Worker;

public interface ISyncAITokenWorker
{
    Task DoWorkAsync();
}

public class SyncAITokenWorker : ISyncAITokenWorker, ISingletonDependency
{
    private readonly ILogger<SyncAITokenWorker> _logger;
    private readonly ISynchronizeAITokenAppService _synchronizeAITokenAppService;


    public SyncAITokenWorker(
        ILogger<SyncAITokenWorker> logger,
        ISynchronizeAITokenAppService synchronizeAITokenAppService)
    {
        _logger = logger;
        _synchronizeAITokenAppService = synchronizeAITokenAppService;
    }

    public async Task DoWorkAsync()
    {
        _logger.LogInformation("Executing sync AI token job");
        var syncSymbols = await _synchronizeAITokenAppService.SearchUnfinishedSynchronizeAITokenAsync();

        var tasks = new List<Task>();

        foreach (var syncSymbol in syncSymbols)
        {
            //await _synchronizeAITokenAppService.ExecuteJobAsync(syncSymbol); 
            tasks.Add(Task.Run(() => { _synchronizeAITokenAppService.ExecuteJobAsync(syncSymbol); }));
        }

        await Task.WhenAll(tasks);
    }
}