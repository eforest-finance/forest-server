using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Order.Etos;
using NFTMarketServer.Order.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core.Order;

public class NFTOrderIndexHandler : IDistributedEventHandler<NFTOrderEto>, ITransientDependency
{
    private readonly INESTRepository<NFTOrderIndex, Guid> _repository;
    private readonly ILogger<NFTOrderIndexHandler> _logger;
    private readonly IObjectMapper _objectMapper;

    public NFTOrderIndexHandler(INESTRepository<NFTOrderIndex, Guid> repository, ILogger<NFTOrderIndexHandler> logger, IObjectMapper objectMapper)
    {
        _repository = repository;
        _logger = logger;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(NFTOrderEto eventData)
    {
        var index = _objectMapper.Map<NFTOrderEto, NFTOrderIndex>(eventData);
        var oldIndex = await _repository.GetAsync(index.Id);
        if (oldIndex != null && oldIndex.OrderStatus > index.OrderStatus)
        {
            return;
        }
        await _repository.AddOrUpdateAsync(index);
    }
}