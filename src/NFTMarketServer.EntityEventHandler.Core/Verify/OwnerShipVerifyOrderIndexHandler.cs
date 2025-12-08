using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Symbol.Etos;
using NFTMarketServer.Symbol.Index;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.EntityEventHandler.Core.Verify;

public class OwnerShipVerifyOrderIndexHandler : VerifyIndexHandlerBase, IDistributedEventHandler<OwnerShipVerifyOrderEto>
{
    private readonly INESTRepository<OwnerShipVerifyOrderIndex, Guid> _ownerShipVerifyRepository;
    private readonly ILogger<OwnerShipVerifyOrderIndexHandler> _logger;

    public OwnerShipVerifyOrderIndexHandler(INESTRepository<OwnerShipVerifyOrderIndex, Guid> ownerShipVerifyRepository, ILogger<OwnerShipVerifyOrderIndexHandler> logger)
    {
        _ownerShipVerifyRepository = ownerShipVerifyRepository;
        _logger = logger;
    }

    public async Task HandleEventAsync(OwnerShipVerifyOrderEto eventData)
    {
        var index = ObjectMapper.Map<OwnerShipVerifyOrderEto, OwnerShipVerifyOrderIndex>(eventData);
        if (index.Id == Guid.Empty)
        {
            index.Id = Guid.NewGuid();
        }
        await _ownerShipVerifyRepository.AddOrUpdateAsync(index);
    }
}