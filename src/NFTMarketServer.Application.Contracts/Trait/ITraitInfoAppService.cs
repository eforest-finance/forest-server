using System.Threading.Tasks;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Trait;

public interface ITraitInfoAppService
{
    public Task<NFTTraitsInfoDto> QueryNFTTraitsInfoAsync(QueryNFTTraitsInfoInput input);

    public Task<PagedResultDto<NFTCollectionTraitInfoDto>> QueryNFTCollectionTraitsInfoAsync(
        QueryNFTCollectionTraitsInfoInput input);

    public Task<CollectionGenerationInfoDto> QueryCollectionGenerationInfoAsync(
        QueryCollectionGenerationInfoInput input);
    
    public Task<CollectionRarityInfoDto> QueryCollectionRarityInfoAsync(
        QueryCollectionRarityInfoInput input);
}