using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Etos;
using NFTMarketServer.Dealer.Index;
using NFTMarketServer.HandleException;
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
    [ExceptionHandler(typeof(Exception),
        Message = "ContractInvokeChangedHandler.HandleEventAsync invoke contract for",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new[] { "eventData"}
    )]
    public virtual async Task HandleEventAsync(ContractInvokeChangedEto eventData)
    {
        var grainDto = eventData.ContractInvokeGrainDto;
        var index = _objectMapper.Map<ContractInvokeGrainDto, ContractInvokeIndex>(grainDto);
        await _contractInvokeRepository.AddOrUpdateAsync(index);
    }
}