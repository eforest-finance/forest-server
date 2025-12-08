using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Order.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.Order.Handler;

public class CreateSeedResultEventHandler : IDistributedEventHandler<CreateSeedResultEvent>, ITransientDependency
{
    private readonly INESTRepository<NFTOrderIndex, Guid> _repository;
    private readonly ILogger<CreateSeedResultEventHandler> _logger;
    private readonly IOrderAppService _orderAppService;
    private readonly IPayCallbackAppService _payCallbackAppService;

    public CreateSeedResultEventHandler(INESTRepository<NFTOrderIndex, Guid> repository, ILogger<CreateSeedResultEventHandler> logger,
        IOrderAppService orderAppService, IPayCallbackAppService payCallbackAppService)
    {
        _repository = repository;
        _logger = logger;
        _orderAppService = orderAppService;
        _payCallbackAppService = payCallbackAppService;
    }

    public async Task HandleEventAsync(CreateSeedResultEvent resultEventData)
    {
        _logger.LogInformation("received IssueSeedEvent: {0}", JsonConvert.SerializeObject(resultEventData));
        var nftOrderIndex = await _repository.GetAsync(resultEventData.Id);
        if (nftOrderIndex == null || nftOrderIndex.OrderStatus > OrderStatus.Notifying)
        {
            return;
        }

        nftOrderIndex.OrderStatus = resultEventData.Success ? OrderStatus.NotifySuccess : OrderStatus.NotifyFail;
        nftOrderIndex.NftReleaseTransactionId = resultEventData.TransactionId;
        nftOrderIndex.LastModifyTime = DateTime.UtcNow.ToTimestamp().Seconds;
        await _orderAppService.AddOrUpdateNFTOrderAsync(nftOrderIndex);
        await _payCallbackAppService.NotifyReleaseResultAsync(nftOrderIndex);
    }
}