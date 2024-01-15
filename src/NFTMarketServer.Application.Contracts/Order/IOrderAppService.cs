using System.Threading.Tasks;
using NFTMarketServer.Order.Dto;
using NFTMarketServer.Order.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Order;

public interface IOrderAppService
{
    Task<CreateOrderResultDto> CreateOrderAsync(CreateOrderInput input);

    Task<NFTOrderDto> SearchOrderAsync(SearchOrderInput input);

    Task AddOrUpdateNFTOrderAsync(NFTOrder nftOrder);
}