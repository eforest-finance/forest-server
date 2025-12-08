using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Activity.Index;

namespace NFTMarketServer.Activity.Provider;

public interface IActivityProvider
{
    public Task<IndexerActivities> GetActivityListAsync(List<string> address, List<SymbolMarketActivityType> types, int skipCount,
        int maxResultCount);
}