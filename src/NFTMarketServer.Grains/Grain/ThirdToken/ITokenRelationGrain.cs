namespace NFTMarketServer.Grains.Grain.ThirdToken;

public interface ITokenRelationGrain : IGrainWithStringKey
{
    Task<GrainResultDto<TokenRelationGrainDto>> CreateTokenRelationAsync(TokenRelationGrainDto input);
    Task<GrainResultDto<TokenRelationGrainDto>> BoundAsync();
    Task<GrainResultDto<TokenRelationGrainDto>> UnBoundAsync();
}