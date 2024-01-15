using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public interface INFTActivityAppService
{
    Task<PagedResultDto<NFTActivityDto>> GetListAsync(GetActivitiesInput input);
}