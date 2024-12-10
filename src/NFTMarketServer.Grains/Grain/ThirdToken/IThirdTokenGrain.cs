namespace NFTMarketServer.Grains.Grain.ThirdToken;

public interface IThirdTokenGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ThirdTokenGrainDto>> CreateThirdTokenAsync(ThirdTokenGrainDto input);
    Task<GrainResultDto<ThirdTokenGrainDto>> FinishedAsync();
}