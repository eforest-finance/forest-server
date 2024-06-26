using System.Threading.Tasks;
using NFTMarketServer.NFT;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Ai
{
    public interface IAiAppService
    {
        Task<PagedResultDto<CreateAiArtDto>> CreateAiArtAsync(CreateAiArtInput input);
        
        Task<PagedResultDto<CreateAiArtDto>> CreateAiArtRetryAsync(CreateAiArtRetryInput input);
        
        Task<PagedResultDto<AiArtFailDto>> QueryAiArtFailAsync(QueryAiArtFailInput input);
        
        Task<PagedResultDto<CreateAiArtDto>> GetAiArtsAsync(GetAIArtsInput input); 
        
        Task<CreateAiResultDto> CreateAiArtAsyncV2(CreateAiArtInput input);

        Task<ResultDto<string>> UseAIArtsAsync(UseAIArtsInput input);
        
        ResultDto<string> GETAIPrompts();

    }
}