using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.HandleException;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.ThirdToken.Etos;
using NFTMarketServer.ThirdToken.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core.ThirdToken;

public class TokenRelationHandle : IDistributedEventHandler<TokenRelationEto>, ISingletonDependency
{
    
    private readonly INESTRepository<TokenRelationIndex, string> _repository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<TokenRelationHandle> _logger;

    public TokenRelationHandle(INESTRepository<TokenRelationIndex, string> repository, IObjectMapper objectMapper, ILogger<TokenRelationHandle> logger)
    {
        _repository = repository;
        _objectMapper = objectMapper;
        _logger = logger;
    }


    [ExceptionHandler(typeof(Exception),
        Message = "TokenRelationHandle fail",
        LogOnly = true,
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new[] { "eventData" }
    )]
    public virtual async Task HandleEventAsync(TokenRelationEto eventData)
    {
        var index = _objectMapper.Map<TokenRelationEto, TokenRelationIndex>(eventData);

        await _repository.AddOrUpdateAsync(index);

        if (index != null)
        {
            _logger.LogDebug("TokenRelationHandle add or update success: {eventData}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}