using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Provider;

namespace NFTMarketServer.NFT;

public class UserBalanceSyncDataService : ScheduleSyncDataService
{
    private readonly IChainAppService _chainAppService;
    
    public UserBalanceSyncDataService(ILogger<ScheduleSyncDataService> logger,
        IGraphQLProvider graphQlProvider, IChainAppService chainAppService) : base(logger, graphQlProvider, chainAppService)
    {
        _chainAppService = chainAppService;
    }

    public override Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.UserBalanceSync;
    }
}