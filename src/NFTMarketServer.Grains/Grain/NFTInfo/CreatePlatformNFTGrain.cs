using AElf.ExceptionHandler;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Users.Dto;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.NFTInfo;

public class CreatePlatformNFTGrain : Grain<CreatePlatformNFTState>, ICreatePlatformNFTGrain
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<CreatePlatformNFTGrain> _logger;

    public CreatePlatformNFTGrain(IObjectMapper objectMapper,
        ILogger<CreatePlatformNFTGrain> logger)
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
    [ExceptionHandler(typeof(Exception),
        Message = "CreatePlatformNFTGrain.SaveCreatePlatformNFTAsync is fail", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"createPlatformNFTGrainInput"}
    )]
    public virtual async Task<GrainResultDto<CreatePlatformNFTGrainDto>> SaveCreatePlatformNFTAsync(CreatePlatformNFTGrainInput createPlatformNFTGrainInput)
    {
        if (State == null || State.Address.IsNullOrEmpty())
        {
            State = new CreatePlatformNFTState()
            {
                Address = createPlatformNFTGrainInput.Address,
                Count = 1
            };
        }
        else if(createPlatformNFTGrainInput.IsBack)
        {
            State.Count -= 1;
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
    [ExceptionHandler(typeof(Exception),
        Message = "CreatePlatformNFTGrain.GetCreatePlatformNFTAsync is fail", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow)
    )]
    public virtual async Task<GrainResultDto<CreatePlatformNFTGrainDto>> GetCreatePlatformNFTAsync()
    {
        return new GrainResultDto<CreatePlatformNFTGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<CreatePlatformNFTState, CreatePlatformNFTGrainDto>(State)
        };
    }
}