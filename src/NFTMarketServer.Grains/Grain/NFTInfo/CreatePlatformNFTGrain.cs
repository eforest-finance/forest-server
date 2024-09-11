using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
        try
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
        catch (Exception e)
        {
            _logger.LogError(e, "SaveCreatePlatformNFTAsync Exception input:{A} errMsg:{C}",JsonConvert.SerializeObject(createPlatformNFTGrainInput) ,e.Message);
            throw e;
        }

    }
    
    public async Task<GrainResultDto<CreatePlatformNFTGrainDto>> GetCreatePlatformNFTAsync()
    {
        try
        {
            return new GrainResultDto<CreatePlatformNFTGrainDto>()
            {
                Success = true,
                Data = _objectMapper.Map<CreatePlatformNFTState, CreatePlatformNFTGrainDto>(State)
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetCreatePlatformNFTAsync Exception errMsg:{C}",e.Message);
            throw e;
        }
    }
}