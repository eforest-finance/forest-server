using Orleans;

namespace NFTMarketServer.Grains.Grain.Verify;

public interface IOwnerShipVerifyOrderGrain : IGrainWithStringKey
{
    Task<GrainResultDto<OwnerShipVerifyOrderGrainDto>> GetAsync();
    Task<GrainResultDto<OwnerShipVerifyOrderGrainDto>> AddOrUpdateAsync(OwnerShipVerifyOrderGrainDto dto);
}