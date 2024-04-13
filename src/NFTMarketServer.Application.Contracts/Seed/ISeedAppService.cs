using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Synchronize.Dto;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Seed;

public interface ISeedAppService
{
    Task<PagedResultDto<SpecialSeedDto>> GetSpecialSymbolListAsync(QuerySpecialListInput input);
    
    Task<PagedResultDto<BiddingSeedDto>> GetBiddingSeedsAsync(GetBiddingSeedsInput input);

    Task<CreateSeedResultDto> CreateSeedAsync(CreateSeedDto input);

    // Task<RegularPricePayInfoDto> GetSymbolRegularPriceAsync(GetRegularPricePayInfoPayInput input);

    Task<BidPricePayInfoDto> GetSymbolBidPriceAsync(QueryBidPricePayInfoInput input);

    Task<SeedDto> SearchSeedInfoAsync(SearchSeedInput input);
    Task<SeedDto> GetSeedInfoAsync(QuerySeedInput input);
    Task<PagedResultDto<SeedDto>> MySeedAsync(MySeedInput input);
    
    Task<TransactionFeeDto> GetTransactionFeeAsync();
    
    Task AddOrUpdateTsmSeedInfoAsync(SeedDto seedDto);
    
    Task AddOrUpdateSeedSymbolAsync(SeedSymbolIndex seedSymbol);
    Task UpdateSeedRankingWeightAsync(List<SeedRankingWeightDto> input);
    Task<PagedResultDto<SeedRankingWeightDto>> GetSeedRankingWeightInfosAsync();

    Task<long> CalCollectionItemSupplyTotalAsync(string chainId);
}