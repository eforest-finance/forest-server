using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.TreeGame.Provider;

public interface ITreeGamePointsRecordProvider
{
    public Task<IndexerTreePointsRecordPage> GetSyncTreePointsRecordsAsync(long startBlockHeight ,long endBlockHeight, string chainId);

    public Task<IndexerTreePointsRecordPage> GetTreePointsRecordsAsync(List<string> addresses, long minTimestamp ,long maxTimestamp);

    
}