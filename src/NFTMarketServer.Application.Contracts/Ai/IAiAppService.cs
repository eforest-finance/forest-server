using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Ai
{
    public interface IAiAppService
    {
        Task<PagedResultDto<string>> CreateAiArtAsync(CreateAiArtInput input);
    }
}