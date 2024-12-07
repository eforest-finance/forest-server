using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Synchronize.Dto;

namespace NFTMarketServer.Synchronize;

public interface ISynchronizeAppService
{
    Task<SyncResultDto> GetSyncResultByTxHashAsync(GetSyncResultByTxHashDto input);
    Task<SyncResultDto> GetSyncResultForAuctionSeedByTxHashAsync(GetSyncResultByTxHashDto input);
    Task<SendNFTSyncResponseDto> SendNFTSyncAsync(SendNFTSyncDto input);
    
    Task SendSeedMainChainCreateSyncAsync(IndexerSeedMainChainChange input);
    
    Task<SendNFTSyncResponseDto> AddAITokenSyncJobAsync(SendNFTSyncDto input);

}