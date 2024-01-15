using NFTMarketServer.Grains.State.Users;
using NFTMarketServer.Users.Dto;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Users;

public class UserGrain : Grain<UserState>, IUserGrain
{
    private readonly IObjectMapper _objectMapper;

    public UserGrain(IObjectMapper objectMapper)
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
    public async Task<GrainResultDto<UserGrainDto>> UpdateUserAsync(UserGrainDto input)
    {
        State = _objectMapper.Map<UserGrainDto, UserState>(input);
        if (State.Id == Guid.Empty)
        {
            State.Id = this.GetPrimaryKey();
        }

        await WriteStateAsync();

        return new GrainResultDto<UserGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserState, UserGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<UserGrainDto>> SaveUserSourceAsync(UserSourceInput userSourceInput)
    {
        if (State == null || State.Id == Guid.Empty)
        {
            State = new UserState
            {
                Id = userSourceInput.UserId,
                CaHash = userSourceInput.CaHash,
                AelfAddress = userSourceInput.AelfAddress,
                CaAddressMain = userSourceInput.CaAddressMain,
                CaAddressSide = userSourceInput.CaAddressSide
            };
        }
        else
        {
            if (String.IsNullOrEmpty(State.AelfAddress))
            {
                State.AelfAddress = userSourceInput.AelfAddress;
            }

            if (String.IsNullOrEmpty(State.CaHash))
            {
                State.CaHash = userSourceInput.CaHash;
            }
            
            if (String.IsNullOrEmpty(State.CaAddressMain))
            {
                State.CaAddressMain = userSourceInput.CaAddressMain;
            }

            State.CaAddressSide ??= new Dictionary<string, string>();
            if (userSourceInput.CaAddressSide != null)
            {
                State.CaAddressSide = State.CaAddressSide.Union(userSourceInput.CaAddressSide).ToDictionary(x => x.Key, x => x.Value);
            }
        }

        await WriteStateAsync();
        return new GrainResultDto<UserGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserState, UserGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<UserGrainDto>> GetUserAsync()
    {
        return new GrainResultDto<UserGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<UserState, UserGrainDto>(State)
        };
    }
}