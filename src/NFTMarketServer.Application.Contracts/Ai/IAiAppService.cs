using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Ai.Index;
using NFTMarketServer.NFT;
using NFTMarketServer.Ai;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Ai
{
    public interface IAiAppService
    {
        Task<PagedResultDto<CreateAiArtDto>> CreateAiArtAsync(CreateAiArtInput input);
        
        Task<PagedResultDto<AIImageIndex>> GetAiArtsAsync(GetAIArtsInput input); 
        Task<ResultDto<string>> UseAIArtsAsync(UseAIArtsInput input);
        
        ResultDto<string> GETAIPrompts();

    }
}