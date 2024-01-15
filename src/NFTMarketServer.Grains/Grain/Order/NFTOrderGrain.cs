using Orleans;
using NFTMarketServer.Grains.State.Order;
using NFTMarketServer.Order;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Grains.Grain.Order;

public class NFTOrderGrain : Grain<NFTOrderState>, INFTOrderGrain
{
    private readonly IObjectMapper _objectMapper;

    public NFTOrderGrain(IObjectMapper objectMapper)
    {
        _objectMapper = objectMapper;
    }

    public async Task<GrainResultDto<NFTOrderGrainDto>> GetAsync()
    {
        await ReadStateAsync();
        if (State.Id == Guid.Empty)
        {
            return new GrainResultDto<NFTOrderGrainDto>
            {
                Success = false
            };
        }
        return new GrainResultDto<NFTOrderGrainDto>
        {
            Success = true,
            Data = _objectMapper.Map<NFTOrderState, NFTOrderGrainDto>(State)
        }; 
    }

    public async Task<GrainResultDto<Dictionary<OrderStatus, OrderStatusInfo>>> GetOrderStatusInfoMapAsync()
    {
        await ReadStateAsync();
        if (State.Id == Guid.Empty)
        {
            return new GrainResultDto<Dictionary<OrderStatus, OrderStatusInfo>>
            {
                Success = false
            };
        }
        return new GrainResultDto<Dictionary<OrderStatus, OrderStatusInfo>>
        {
            Success = true,
            Data = State.OrderStatusInfoMap
        }; 
    }

    public async Task<GrainResultDto<NFTOrderGrainDto>> AddOrUpdateAsync(NFTOrderGrainDto nftOrderGrainDto)
    {
        if (State.Id == Guid.Empty)
        {
            State = _objectMapper.Map<NFTOrderGrainDto, NFTOrderState>(nftOrderGrainDto);
            State.OrderStatusInfoMap = new Dictionary<OrderStatus, OrderStatusInfo>();
        }
        else
        {
            _objectMapper.Map(nftOrderGrainDto, State);
        }
        if (!State.OrderStatusInfoMap.ContainsKey(nftOrderGrainDto.OrderStatus))
        {
            State.OrderStatusInfoMap[nftOrderGrainDto.OrderStatus] = new OrderStatusInfo
            {
                OrderStatus = nftOrderGrainDto.OrderStatus,
                Timestamp = nftOrderGrainDto.LastModifyTime,
                ExternalInfo = nftOrderGrainDto.ExternalInfo
            };
        }

        await WriteStateAsync();
        return new GrainResultDto<NFTOrderGrainDto>
        {
            Success = true,
            Data = nftOrderGrainDto
        };
    }
}