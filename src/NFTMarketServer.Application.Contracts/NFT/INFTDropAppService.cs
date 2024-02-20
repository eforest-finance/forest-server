using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public interface INFTDropAppService
    {
        Task CreateNFTDropExtensionAsync(CreateNFTDropInput input);
        Task<PagedResultDto<NFTDropIndexDto>> GetNFTDropListAsync(GetNFTDropListInput input);
        Task<List<RecommendedNFTDropIndexDto>> GetRecommendedNFTDropListAsync();
        Task<NFTDropDetailDto> GetNFTDropDetailAsync(GetNFTDropDetailInput input);
        Task<NFTDropQuotaDto> GetNFTDropQuotaAsync(GetNFTDropQuotaInput input);
    }
    
}
