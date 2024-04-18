using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Chain;
using NFTMarketServer.Inscription;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer;

public class MockGraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    public Task<long> GetLastEndHeightAsync(string chainId, string type)
    {
        throw new System.NotImplementedException();
    }

    public Task SetLastEndHeightAsync(string chainId, string type, long height)
    {
        throw new System.NotImplementedException();
    }

    public Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<AuctionInfoDto>> GetSyncSymbolAuctionRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        throw new System.NotImplementedException();
    }

    public Task<SeedSymbolIndex> GetSyncSeedSymbolRecordAsync(string nftInfoId, string chainId)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<BidInfoDto>> GetSyncSymbolBidRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        throw new System.NotImplementedException();
    }

    public async Task<SeedInfoDto> GetSeedInfoAsync(string symbol)
    {
        return new SeedInfoDto()
        {
            Symbol = symbol,
            SeedType = SeedType.Regular,
            Status = SeedStatus.AVALIABLE,
            TokenPrice = new PriceInfo()
            {
                Amount = 100,
                Symbol = "ELF"
            }
        };
    }

    public Task<List<SeedDto>> GetSyncTsmSeedRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<SeedPriceDto>> GetSeedPriceDtoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<UniqueSeedPriceDto>> GetUniqueSeedPriceDtoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<NFTInfoIndex>> GetSyncNftInfoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<SeedSymbolIndex>> GetSyncSeedSymbolRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        throw new System.NotImplementedException();
    }

    public Task<List<InscriptionDto>> GetIndexInscriptionAsync(string chainId, long beginBlockHeight,
        long endBlockHeight, int skipCount, int maxResultCount)
    {
        throw new System.NotImplementedException();
    }
    public Task<long> GetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType)
    {
        throw new System.NotImplementedException();
    }

    public Task SetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType, long height)
    {
        throw new System.NotImplementedException();
    }

    public Task<NFTInfoIndex> GetSyncNftInfoRecordAsync(string nftInfoId, string chainId)
    {
        throw new System.NotImplementedException();
    }
}