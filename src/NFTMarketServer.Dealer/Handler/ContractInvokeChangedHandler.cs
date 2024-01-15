using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Etos;
using NFTMarketServer.Dealer.Index;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.Handler;

public class ContractInvokeChangedHandler : IDistributedEventHandler<ContractInvokeChangedEto>, ISingletonDependency
{
    private readonly ILogger<ContractInvokeChangedHandler> _logger;
    private readonly INESTRepository<ContractInvokeIndex, Guid> _contractInvokeRepository;
    private readonly IObjectMapper _objectMapper;

    public ContractInvokeChangedHandler(ILogger<ContractInvokeChangedHandler> logger,
        INESTRepository<ContractInvokeIndex, Guid> contractInvokeRepository, IObjectMapper objectMapper)
    {
        _logger = logger;
        _contractInvokeRepository = contractInvokeRepository;
        _objectMapper = objectMapper;
    }

    public async Task HandleEventAsync(ContractInvokeChangedEto eventData)
    {
        var grainDto = eventData.ContractInvokeGrainDto;
        try
        {
            // save index data to ES
            var index = _objectMapper.Map<ContractInvokeGrainDto, ContractInvokeIndex>(grainDto);
            await _contractInvokeRepository.AddOrUpdateAsync(index);
        }
        catch (UserFriendlyException e)
        {
            _logger.LogWarning(e, "invoke contract for {BizType}_{BizId} ({Guid}) FAILED",
                eventData.ContractInvokeGrainDto.BizType, eventData.ContractInvokeGrainDto.BizId, grainDto.Id);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "invoke contract for {BizType}_{BizId} ({Guid})  ERROR",
                eventData.ContractInvokeGrainDto.BizType, eventData.ContractInvokeGrainDto.BizId, grainDto.Id);
        }
    }
}