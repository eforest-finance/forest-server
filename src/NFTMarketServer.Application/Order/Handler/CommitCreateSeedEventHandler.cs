using System;
using System.Threading.Tasks;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Dealer.ContractInvoker;
using NFTMarketServer.Dealer.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus;

namespace NFTMarketServer.Order.Handler;

public class CommitCreateSeedEventHandler : ILocalEventHandler<CommitCreateSeedEvent>, ITransientDependency
{
    private readonly ILogger<CommitCreateSeedEventHandler> _logger;
    private readonly IOrderAppService _orderAppService;
    private readonly IContractInvokerFactory _contractInvokerFactory;

    public CommitCreateSeedEventHandler(ILogger<CommitCreateSeedEventHandler> logger, IOrderAppService orderAppService, IContractInvokerFactory contractInvokerFactory)
    {
        _logger = logger;
        _orderAppService = orderAppService;
        _contractInvokerFactory = contractInvokerFactory;
    }

    public async Task HandleEventAsync(CommitCreateSeedEvent eventData)
    {
        _logger.LogInformation("CommitCreateSeedEvent: {0}", JsonConvert.SerializeObject(eventData));
        await _contractInvokerFactory.Invoker(BizType.CreateSeed.ToString()).InvokeAsync(new CreateSeedBizDto
        {
            OrderId = eventData.Id.ToString(),
            ChainId = eventData.ChainId,
            Symbol = eventData.NftSymbol,
            Address = eventData.Address
        });
        eventData.OrderStatus = OrderStatus.Notifying;
        eventData.LastModifyTime = DateTime.UtcNow.ToTimestamp().Seconds;
        await _orderAppService.AddOrUpdateNFTOrderAsync(eventData);
    }
}