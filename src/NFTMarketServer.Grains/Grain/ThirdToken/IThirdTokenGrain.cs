namespace NFTMarketServer.Grains.Grain.ThirdToken;

public interface IThirdTokenGrain : IGrainWithStringKey
{
    Task<GrainResultDto<ThirdTokenGrainDto>> CreateThirdTokenAsync(ThirdTokenGrainDto input);
    Task<GrainResultDto<ThirdTokenGrainDto>> FinishedAsync(string deployedTokenContractAddress, string associatedTokenAccount);
    Task<GrainResultDto<ThirdTokenGrainDto>> GetThirdTokenAsync();
}