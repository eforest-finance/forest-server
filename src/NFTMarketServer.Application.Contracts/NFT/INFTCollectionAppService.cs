using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public interface INFTCollectionAppService
    {
        Task<PagedResultDto<NFTCollectionIndexDto>> GetNFTCollectionsAsync(GetNFTCollectionsInput input);
        
        Task<PagedResultDto<SearchNFTCollectionsDto>> SearchNFTCollectionsAsync(SearchNFTCollectionsInput input);
        
        Task<List<RecommendedNFTCollectionsDto>> GetRecommendedNFTCollectionsAsync();

        Task<NFTCollectionIndexDto> GetNFTCollectionAsync(string id);
        Task CreateCollectionExtensionAsync(CreateCollectionExtensionInput input);
        
        Task CollectionMigrateAsync(CollectionMigrateInput input);
    }
}