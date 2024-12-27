using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer;

public interface IScheduleSyncDataContext
{
    Task DealAsync(BusinessQueryChainType businessQueryChainType, bool resetHeightFlag, long resetHeight);
}

public class ScheduleSyncDataContext : ITransientDependency, IScheduleSyncDataContext
{
    private readonly Dictionary<BusinessQueryChainType, IScheduleSyncDataService> _syncDataServiceMap;
    private readonly ILogger<ScheduleSyncDataContext> _logger;

    public ScheduleSyncDataContext(IEnumerable<IScheduleSyncDataService> scheduleSyncDataServices, 
        ILogger<ScheduleSyncDataContext> logger)
    {
        _syncDataServiceMap = scheduleSyncDataServices.ToDictionary(a => a.GetBusinessType(),a => a);
        _logger = logger;
    }

    public async Task DealAsync(BusinessQueryChainType businessType, bool resetHeightFlag, long resetHeight)
    {
        // var stopwatch = Stopwatch.StartNew();
        // await _syncDataServiceMap.GetOrDefault(businessType).DealDataAsync(resetHeightFlag, resetHeight);
        // stopwatch.Stop();
        // _logger.LogInformation("It took {Elapsed} ms to execute synchronized data for businessType: {businessType}",
        //     stopwatch.ElapsedMilliseconds, businessType);
    }
}