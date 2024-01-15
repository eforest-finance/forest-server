using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Chain;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Inscription;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;

namespace NFTMarketServer.Provider;

public interface IGraphQLProvider
{
    public Task<long> GetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType);
    public Task SetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType, long height);
    public Task<long> GetIndexBlockHeightAsync(string chainId);
    
    
    Task<List<AuctionInfoDto>> GetSyncSymbolAuctionRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    
    Task<List<BidInfoDto>> GetSyncSymbolBidRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    Task<SeedInfoDto> GetSeedInfoAsync(string symbol);


    Task<List<SeedDto>> GetSyncTsmSeedRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    
    
    Task<List<SeedPriceDto>> GetSeedPriceDtoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    
    Task<List<UniqueSeedPriceDto>> GetUniqueSeedPriceDtoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);

    Task<List<NFTInfoIndex>> GetSyncNftInfoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    
    Task<List<SeedSymbolIndex>> GetSyncSeedSymbolRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight);
    Task<List<InscriptionDto>> GetIndexInscriptionAsync(string chainId, long beginBlockHeight,
        long endBlockHeight, int skipCount, int maxResultCount);

}