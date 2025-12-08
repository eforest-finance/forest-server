using System.Threading.Tasks;
using NFTMarketServer.Order.Dto;
using NFTMarketServer.Order.Index;

namespace NFTMarketServer.Order;

public interface IPayCallbackAppService
{
    Task<bool> PortkeyOrderCallbackAsync(PortkeyCallbackInput input);
    Task NotifyReleaseResultAsync(NFTOrderIndex nftIndex);
}