using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Users.Dto;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class CreatePlatformNFTGrain : Grain<CreatePlatformNFTState>, ICreatePlatformNFTGrain
{
    private readonly IObjectMapper _objectMapper;

    public CreatePlatformNFTGrain(IObjectMapper objectMapper)
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

    public async Task<GrainResultDto<CreatePlatformNFTGrainDto>> SaveCreatePlatformNFTAsync(CreatePlatformNFTGrainInput createPlatformNFTGrainInput)
    {
        if (State == null || State.Address.IsNullOrEmpty())
        {
            State = new CreatePlatformNFTState()
            {
                Address = createPlatformNFTGrainInput.Address,
                Count = 1
            };
        }
        else
        {
            State.Count += 1;
        }

        await WriteStateAsync();
        return new GrainResultDto<CreatePlatformNFTGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CreatePlatformNFTState, CreatePlatformNFTGrainDto>(State)
        };
    }
    
    public async Task<GrainResultDto<CreatePlatformNFTGrainDto>> GetCreatePlatformNFTAsync()
    {
        return new GrainResultDto<CreatePlatformNFTGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CreatePlatformNFTState, CreatePlatformNFTGrainDto>(State)
        };
    }
}