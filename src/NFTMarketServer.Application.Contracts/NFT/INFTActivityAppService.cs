using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public interface INFTActivityAppService
{
    Task<PagedResultDto<NFTActivityDto>> GetListAsync(GetActivitiesInput input);
    
    Task<PagedResultDto<NFTActivityDto>> GetCollectionActivityListAsync(GetCollectionActivityListInput input);
    
    Task<PagedResultDto<CollectedCollectionActivitiesDto>> GetCollectedCollectionActivitiesAsync(GetCollectedCollectionActivitiesInput input);
}