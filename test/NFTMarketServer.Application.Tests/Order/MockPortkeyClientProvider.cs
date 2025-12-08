using System;
using System.Threading.Tasks;
using NFTMarketServer.Helper;
using NFTMarketServer.Order.Dto;

namespace NFTMarketServer.Order;

public class MockPortkeyClientProvider : IPortkeyClientProvider
{
    public async Task<PortkeyCreateOrderResultDto> CreateOrderAsync(PortkeyCreateOrderParam param)
    {
        return new PortkeyCreateOrderResultDto
        {
            OrderId = GuidHelper.UniqId(param.MerchantOrderId.ToString(), param.NftSymbol)
        };
    }

    public async Task<PortkeySearchOrderResultDto> SearchOrderAsync(PortkeySearchOrderParam param)
    {
        return new PortkeySearchOrderResultDto()
        {
            MerchantOrderId = param.MerchantOrderId,
            MerchantName = param.MerchantName,
            Status = nameof(PortkeyOrderStatus.Pending)
        };
    }

    public async Task<bool> NotifyReleaseResultAsync(PortkeyNotifyReleaseResultParam param)
    {
        return true;
    }
}