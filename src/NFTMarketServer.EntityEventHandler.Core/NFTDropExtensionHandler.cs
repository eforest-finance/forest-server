using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTDropExtensionHandler : IDistributedEventHandler<NFTDropExtraEto>, ITransientDependency
{
    private readonly INESTRepository<NFTDropExtensionIndex, string> _nftDropExtensionRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NFTDropExtensionHandler> _logger;

    public NFTDropExtensionHandler(
        INESTRepository<NFTDropExtensionIndex, string> nftDropExtensionRepository,
        IObjectMapper objectMapper,
        ILogger<NFTDropExtensionHandler> logger)
    {
        _nftDropExtensionRepository = nftDropExtensionRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    [ExceptionHandler(typeof(Exception),
        Message = "NFTDropExtensionHandler.HandleEventAsync nftInfoExtension information add or update fail", 
        LogOnly = true,
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new []{"eventData" }
    )]
    public virtual async Task HandleEventAsync(NFTDropExtraEto eventData)
    {
        var nftDropExtensionIndex = _objectMapper.Map<NFTDropExtraEto, NFTDropExtensionIndex>(eventData);

        await _nftDropExtensionRepository.AddOrUpdateAsync(nftDropExtensionIndex);

        if (nftDropExtensionIndex != null)
        {
            _logger.LogDebug("nftInfoExtension information add or update success: {userInformation}",
                JsonConvert.SerializeObject(nftDropExtensionIndex));
        }
    }
}