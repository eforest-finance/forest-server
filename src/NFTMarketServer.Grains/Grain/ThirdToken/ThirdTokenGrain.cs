using NFTMarketServer.Common;
using NFTMarketServer.Grains.State.ThirdToken;
using NFTMarketServer.ThirdToken.Index;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.ThirdToken;

public class ThirdTokenGrain : Grain<ThirdTokenState>, IThirdTokenGrain
{
    private readonly IObjectMapper _objectMapper;

    public ThirdTokenGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<ThirdTokenGrainDto>> CreateThirdTokenAsync(ThirdTokenGrainDto input)
    {
        State = _objectMapper.Map<ThirdTokenGrainDto, ThirdTokenState>(input);
        State.Id = this.GetPrimaryKeyString();
        State.CreateTime = DateTime.UtcNow.ToUtcMilliSeconds();
        State.ThirdTokenStatus = ThirdTokenStatus.Creating;

        await WriteStateAsync();

        return new GrainResultDto<ThirdTokenGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<ThirdTokenGrainDto, ThirdTokenState>(State)
        };
    }

    public async Task<GrainResultDto<ThirdTokenGrainDto>> FinishedAsync(string deployedTokenContractAddress, string associatedTokenAccount)
    {
        State.ThirdTokenStatus = ThirdTokenStatus.Created;
        //State.TokenContractAddress = deployedTokenContractAddress;
        State.AssociatedTokenAccount = associatedTokenAccount;
        await WriteStateAsync();
        return new GrainResultDto<ThirdTokenGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<ThirdTokenGrainDto, ThirdTokenState>(State)
        };
    }

    public async Task<GrainResultDto<ThirdTokenGrainDto>> GetThirdTokenAsync()
    {
        return new GrainResultDto<ThirdTokenGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<ThirdTokenGrainDto, ThirdTokenState>(State)
        };
    }
}