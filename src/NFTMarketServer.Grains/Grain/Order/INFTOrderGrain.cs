using NFTMarketServer.Grains.State.Order;
using NFTMarketServer.Order;
using Orleans;

namespace NFTMarketServer.Grains.Grain.Order;

public interface INFTOrderGrain : IGrainWithStringKey
{
    Task<GrainResultDto<NFTOrderGrainDto>> GetAsync();

    Task<GrainResultDto<Dictionary<OrderStatus, OrderStatusInfo>>> GetOrderStatusInfoMapAsync();
    Task<GrainResultDto<NFTOrderGrainDto>> AddOrUpdateAsync(NFTOrderGrainDto nftOrderGrainDto);
}