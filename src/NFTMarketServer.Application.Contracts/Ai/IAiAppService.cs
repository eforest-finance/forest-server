using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Ai.Index;
using NFTMarketServer.NFT;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Ai
{
    public interface IAiAppService
    {
        Task<PagedResultDto<string>> CreateAiArtAsync(CreateAiArtInput input);
        
        Task<PagedResultDto<List<AIImageIndex>>> GetAiArtsAsync(GetAIArtsInput input);

    }
}