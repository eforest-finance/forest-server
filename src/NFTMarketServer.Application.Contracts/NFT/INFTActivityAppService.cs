using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Dto;
using NFTMarketServer.NFT.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public interface INFTActivityAppService
{
    Task<PagedResultDto<NFTActivityDto>> GetListAsync(GetActivitiesInput input);
    
    Task<PagedResultDto<NFTActivityDto>> GetCollectionActivityListAsync(GetCollectionActivityListInput input);

    Task<PagedResultDto<CollectedCollectionActivitiesDto>> GetCollectedCollectionActivitiesAsync(
        GetCollectedCollectionActivitiesInput input, List<string> nftInfoIds);
    
    Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedActivityListAsync(
        GetCollectedActivityListDto dto);
    
    Task<Tuple<long, List<NFTActivityIndex>>> GetActivityByIdListAsync(List<string> idList);
}