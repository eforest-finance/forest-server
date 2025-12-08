using NFTMarketServer.Users.Dto;
using Orleans;

namespace NFTMarketServer.Grains.Grain.Users;

public interface IUserGrain : IGrainWithGuidKey
{

    Task<GrainResultDto<UserGrainDto>> UpdateUserAsync(UserGrainDto input);

    Task<GrainResultDto<UserGrainDto>> GetUserAsync();

    Task<GrainResultDto<UserGrainDto>> SaveUserSourceAsync(UserSourceInput userSourceInput);
}