using System.Threading.Tasks;
using NFTMarketServer.Ai;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public interface INFTListingAppService
    {
        Task<PagedResultDto<NFTListingIndexDto>> GetNFTListingsAsync(GetNFTListingsInput input);
        Task<PagedResultDto<CollectedCollectionListingDto>> GetCollectedCollectionListingAsync(GetCollectedCollectionListingsInput input);
        
        Task<ResultDto<string>> StatisticsUserListRecord(GetNFTListingsInput input);

        
    }
}