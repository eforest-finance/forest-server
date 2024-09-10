using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Users.Dto;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class PlatformNFTTokenIdGrain : Grain<PlatformNFTTokenIdState>, IPlatformNFTTokenIdGrain
{
    private readonly IObjectMapper _objectMapper;

    public PlatformNFTTokenIdGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }
    
    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }
    
    public async Task<GrainResultDto<PlatformNFTTokenIdGrainDto>> GetPlatformNFTCurrentTokenIdAsync()
    {
        return new GrainResultDto<PlatformNFTTokenIdGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<PlatformNFTTokenIdState, PlatformNFTTokenIdGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<PlatformNFTTokenIdGrainDto>> SavePlatformNFTTokenIdAsync(PlatformNFTTokenIdGrainInput platformNFTTokenIdGrainInput)
    {
        if (State == null || State.CollectionSymbol.IsNullOrEmpty())
        {
            State = new PlatformNFTTokenIdState()
            {
                CollectionSymbol = platformNFTTokenIdGrainInput.CollectionSymbol,
                TokenID = "1"
            };
        }
        else
        {
            State.TokenID = platformNFTTokenIdGrainInput.TokenId;
        }

        await WriteStateAsync();
        return new GrainResultDto<PlatformNFTTokenIdGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<PlatformNFTTokenIdState, PlatformNFTTokenIdGrainDto>(State)
        };
    }

}