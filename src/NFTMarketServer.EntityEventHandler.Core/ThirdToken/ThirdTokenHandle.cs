using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.HandleException;
using NFTMarketServer.ThirdToken.Etos;
using NFTMarketServer.ThirdToken.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core.ThirdToken;

public class ThirdTokenHandle : IDistributedEventHandler<ThirdTokenEto>, ISingletonDependency
{
    private readonly INESTRepository<ThirdTokenIndex, string> _repository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<ThirdTokenHandle> _logger;

    public ThirdTokenHandle(INESTRepository<ThirdTokenIndex, string> repository, IObjectMapper objectMapper,
        ILogger<ThirdTokenHandle> logger)
    {
        _repository = repository;
        _objectMapper = objectMapper;
        _logger = logger;
    }


    [ExceptionHandler(typeof(Exception),
        Message = "ThirdTokenHandle fail",
        LogOnly = true,
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new[] { "eventData" }
    )]
    public virtual async Task HandleEventAsync(ThirdTokenEto eventData)
    {
        var index = _objectMapper.Map<ThirdTokenEto, ThirdTokenIndex>(eventData);

        await _repository.AddOrUpdateAsync(index);

        if (index != null)
        {
            _logger.LogDebug("ThirdTokenHandle add or update success: {eventData}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}