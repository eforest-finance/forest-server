using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Symbol;
using NFTMarketServer.Symbol.Index;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.OwnerShip.Handler;

public class SetIssueAddressEventHandler : IDistributedEventHandler<SetIssueAddressEvent>
{
    private readonly INESTRepository<OwnerShipVerifyOrderIndex, Guid> _ownerShipVerifyOrderRepository;
    private readonly IVerifyAppService _verifyAppService;
    private readonly ILogger<SetIssueAddressEventHandler> _logger;

    public SetIssueAddressEventHandler(INESTRepository<OwnerShipVerifyOrderIndex, Guid> ownerShipVerifyOrderRepository, IVerifyAppService verifyAppService, ILogger<SetIssueAddressEventHandler> logger)
    {
        _ownerShipVerifyOrderRepository = ownerShipVerifyOrderRepository;
        _verifyAppService = verifyAppService;
        _logger = logger;
    }


    public async Task HandleEventAsync(SetIssueAddressEvent eventData)
    {
        _logger.LogInformation("received event: {0}", JsonConvert.SerializeObject(eventData));
        var orderIndex = await _ownerShipVerifyOrderRepository.GetAsync(eventData.Id);
        if (orderIndex == null)
        {
            return;
        }

        orderIndex.ProposalStatus = eventData.Success ? ProposalStatus.Success : ProposalStatus.Fail;
        orderIndex.ProposalTransactionId = eventData.TransactionId;
        await _verifyAppService.AddOrUpdateOwnerShipVerifyOrderAsync(orderIndex);
    }
}