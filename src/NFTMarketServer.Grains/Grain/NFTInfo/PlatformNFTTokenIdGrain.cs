using Microsoft.Extensions.Logging;
using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Users.Dto;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class PlatformNFTTokenIdGrain : Grain<PlatformNFTTokenIdState>, IPlatformNFTTokenIdGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<PlatformNFTTokenIdGrain> _logger;

    public PlatformNFTTokenIdGrain(IObjectMapper objectMapper, ILogger<PlatformNFTTokenIdGrain> logger)
    {
        _objectMapper = objectMapper;
        _logger = logger;
    }
    
    public override async Task OnActivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("OnActivateAsync()");
        await ReadStateAsync();
        await base.OnActivateAsync(cancellationToken);
    }

    public override async Task OnDeactivateAsync(DeactivationReason reason, CancellationToken token)
    {
        _logger.LogInformation("OnDeactivateAsync({Reason})", reason);
        await WriteStateAsync();
        await base.OnDeactivateAsync(reason, token);
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