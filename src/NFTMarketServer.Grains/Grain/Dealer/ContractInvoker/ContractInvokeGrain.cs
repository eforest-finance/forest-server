using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Grains.State.Dealer;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Dealer.ContractInvoker;

public class ContractInvokeGrain : Grain<ContractInvokeState>, IContractInvokeGrain
{
    private readonly IObjectMapper _objectMapper;

    public ContractInvokeGrain(IObjectMapper objectMapper)
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
    
    public async Task<GrainResultDto<ContractInvokeGrainDto>> UpdateAsync(ContractInvokeGrainDto input)
    {
        State = _objectMapper.Map<ContractInvokeGrainDto, ContractInvokeState>(input);
        State.Id = State.Id == Guid.Empty ? this.GetPrimaryKey() : State.Id;
        var now = DateTime.UtcNow.ToUtcMilliSeconds().ToString();
        State.CreateTime = State.CreateTime.DefaultIfEmpty(now);
        State.UpdateTime = now;
        await WriteStateAsync();
        return new GrainResultDto<ContractInvokeGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<ContractInvokeState, ContractInvokeGrainDto>(State)
        };
    }

    public async Task<GrainResultDto<ContractInvokeGrainDto>> GetAsync()
    {
        return new GrainResultDto<ContractInvokeGrainDto>()
        {
            Success = true,
            Data = _objectMapper.Map<ContractInvokeState, ContractInvokeGrainDto>(State)
        };
    }
}