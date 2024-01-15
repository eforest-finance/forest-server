using System.Collections.Generic;
using System.Threading.Tasks;
using MongoDB.Libmongocrypt;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Seed.Dto;

namespace NFTMarketServer.Seed;

public interface ISeedPriceAppService
{
    Task<UniqueSeedPriceDto> GetUniqueSeedPriceInfoAsync(string id);

    Task AddOrUpdateUniqueSeedPriceInfoAsync(UniqueSeedPriceDto uniqueSeedPriceDto);

    
    Task<SeedPriceDto> GetSeedPriceInfoAsync(string id);

    Task AddOrUpdateSeedPriceInfoAsync(SeedPriceDto seedPriceDto);

    Task<List<SeedDto>> GetUniqueSeedDtoNoBurnListAsync(GetTsmUniqueSeedInfoRequestDto input);

    Task UpdateSeedPriceAsync(SeedDto seedDto);

    Task UpdateUniqueAllNoBurnSeedPriceAsync();
    
    
    Task UpdateDisableSeedPriceAsync();

}