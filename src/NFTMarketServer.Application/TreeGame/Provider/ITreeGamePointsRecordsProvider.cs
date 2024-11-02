using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.TreeGame.Provider;

public interface ITreeGamePointsRecordProvider
{
    public Task<IndexerTreePointsRecordPage> GetSyncTreePointsRecordsAsync(long startBlockHeight ,long endBlockHeight, string chainId);

    
    
}