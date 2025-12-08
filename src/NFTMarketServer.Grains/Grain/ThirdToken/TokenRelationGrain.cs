using NFTMarketServer.Common;
using NFTMarketServer.Grains.State.ThirdToken;
using NFTMarketServer.ThirdToken.Index;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.ThirdToken;

public class TokenRelationGrain : Grain<TokenRelationState>, ITokenRelationGrain
{
    private readonly IObjectMapper _objectMapper;

    public TokenRelationGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public override async Task OnActivateAsync(CancellationToken token)
    {
        await ReadStateAsync();
        await base.OnActivateAsync(token);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
    }

    public async Task<GrainResultDto<TokenRelationGrainDto>> CreateTokenRelationAsync(TokenRelationGrainDto input)
    {
        State = _objectMapper.Map<TokenRelationGrainDto, TokenRelationState>(input);
        State.Id = this.GetPrimaryKeyString();
        State.CreateTime = DateTime.UtcNow.ToUtcMilliSeconds();
        State.RelationStatus = RelationStatus.Binding;

        await WriteStateAsync();

        return new GrainResultDto<TokenRelationGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TokenRelationGrainDto, TokenRelationState>(State)
        };
    }

    public async Task<GrainResultDto<TokenRelationGrainDto>> BoundAsync()
    {
        State.RelationStatus = RelationStatus.Bound;
        State.UpdateTime = DateTime.UtcNow.ToUtcMilliSeconds();
        await WriteStateAsync();
        return new GrainResultDto<TokenRelationGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TokenRelationGrainDto, TokenRelationState>(State)
        };
    }

    public async Task<GrainResultDto<TokenRelationGrainDto>> UnBoundAsync()
    {
        State.RelationStatus = RelationStatus.Unbound;
        State.UpdateTime = DateTime.UtcNow.ToUtcMilliSeconds();
        await WriteStateAsync();
        return new GrainResultDto<TokenRelationGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<TokenRelationGrainDto, TokenRelationState>(State)
        };
    }
}