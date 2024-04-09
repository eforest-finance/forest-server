using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Chain;

namespace NFTMarketServer;

public interface IScheduleSyncDataService
{
    Task<long> SyncIndexerRecordsAsync(string chainId,long lastEndHeight, long newIndexHeight);
     
    Task<List<string>> GetChainIdsAsync();

    BusinessQueryChainType GetBusinessType();
    
    Task DealDataAsync(bool resetBlockHeightFlag, long resetBlockHeight);
}