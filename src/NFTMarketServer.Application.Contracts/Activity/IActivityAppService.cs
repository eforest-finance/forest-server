using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Activity;

public interface IActivityAppService
{
    Task<PagedResultDto<SymbolMarketActivityDto>> GetListAsync(GetActivitiesInput input);
}