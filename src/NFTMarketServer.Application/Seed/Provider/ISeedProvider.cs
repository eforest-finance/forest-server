using System.Threading.Tasks;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Seed.Provider;

public interface ISeedProvider
{
    public Task<SeedInfoDto> SearchSeedInfoAsync(SearchSeedInput input);
    public Task<SeedInfoDto> GetSeedInfoAsync(QuerySeedInput input);
    public Task<MySeedDto> MySeedAsync(MySeedInput input);
    public Task<IndexerSpecialSeeds> GetSpecialSeedsAsync(QuerySpecialListInput input);
}