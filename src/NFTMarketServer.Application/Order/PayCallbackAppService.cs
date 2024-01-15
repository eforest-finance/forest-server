using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.Options;
using NFTMarketServer.Order.Dto;
using NFTMarketServer.Order.Handler;
using NFTMarketServer.Order.Index;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Local;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Order;

[RemoteService(IsEnabled = false)]
public class PayCallbackAppService : ApplicationService, IPayCallbackAppService
{
    private readonly IPortkeyClientProvider _portkeyClientProvider;
    private readonly INESTRepository<NFTOrderIndex, Guid> _nftOrderRepository;
    private readonly IOptionsMonitor<PortkeyOption> _portkeyOptionsMonitor;
    private readonly IOrderAppService _orderAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly ILocalEventBus _localEventBus;

    public PayCallbackAppService(IPortkeyClientProvider portkeyClientProvider, INESTRepository<NFTOrderIndex, Guid> nftOrderRepository, 
        IOptionsMonitor<PortkeyOption> portkeyOptionsMonitor, IOrderAppService orderAppService,
        IObjectMapper objectMapper, ILocalEventBus localEventBus)
    {
        _portkeyClientProvider = portkeyClientProvider;
        _nftOrderRepository = nftOrderRepository;
        _portkeyOptionsMonitor = portkeyOptionsMonitor;
        _orderAppService = orderAppService;
        _objectMapper = objectMapper;
        _localEventBus = localEventBus;
    }

    public async Task<bool> PortkeyOrderCallbackAsync(PortkeyCallbackInput input)
    {
        var portkeyOption = _portkeyOptionsMonitor.CurrentValue;
        AssertHelper.IsTrue(portkeyOption.PublicKey.VerifySignature(input.Signature, input), "signature verify fail");
        PortkeyOrderStatus portkeyOrderStatus;
        AssertHelper.IsTrue(PortkeyOrderStatus.TryParse(input.Status, out portkeyOrderStatus), "invalid order status");

        var nftIndex = await _nftOrderRepository.GetAsync(input.MerchantOrderId);
        AssertHelper.NotNull(nftIndex, "order not existed");
        if (nftIndex.OrderStatus >= OrderStatus.Payed)
        {
            return true;
        }
        AssertHelper.IsTrue(nftIndex.OrderStatus == OrderStatus.UnPay, "invalid order");
        
        var orderDto = await _portkeyClientProvider.SearchOrderAsync(new PortkeySearchOrderParam
        {
            MerchantName = OrderConstants.LocalMerchantName,
            MerchantOrderId = nftIndex.Id,
            OrderId = nftIndex.ThirdPartOrderId,
        });
        AssertHelper.NotNull(orderDto, "search thirdPartOrder fail");
        AssertHelper.IsTrue(orderDto.Status == input.Status, "order status changed");

        switch (portkeyOrderStatus)
        {
            case PortkeyOrderStatus.Pending:
                nftIndex.OrderStatus = OrderStatus.Payed;
                break;
            case PortkeyOrderStatus.Failed:
                nftIndex.OrderStatus = OrderStatus.Failed;
                break;
            case PortkeyOrderStatus.Expired:
                nftIndex.OrderStatus = OrderStatus.Expired;
                break;
            default:
                throw new Exception("invalid order status");
        }

        nftIndex.LastModifyTime = DateTime.UtcNow.ToTimestamp().Seconds;
        await _orderAppService.AddOrUpdateNFTOrderAsync(nftIndex);
        if (portkeyOrderStatus == PortkeyOrderStatus.Pending)
        {
            await _localEventBus.PublishAsync(_objectMapper.Map<NFTOrderIndex, CommitCreateSeedEvent>(nftIndex));
        }
        return true;
    }

    public async Task NotifyReleaseResultAsync(NFTOrderIndex nftIndex)
    {
        AssertHelper.IsTrue(await _portkeyClientProvider.NotifyReleaseResultAsync(new PortkeyNotifyReleaseResultParam
        {
            MerchantName = OrderConstants.LocalMerchantName,
            MerchantOrderId = nftIndex.Id,
            ReleaseTransactionId = nftIndex.NftReleaseTransactionId,
            ReleaseResult = nftIndex.OrderStatus == OrderStatus.NotifySuccess
                ? OrderConstants.Success
                : OrderConstants.Fail
        }), "NotifyRelease fail");

        nftIndex.OrderStatus = OrderStatus.Finished;
        nftIndex.LastModifyTime = DateTime.UtcNow.ToTimestamp().Seconds;
        await _orderAppService.AddOrUpdateNFTOrderAsync(nftIndex);
    }
}