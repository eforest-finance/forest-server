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

public class NFTInfoExtensionHandler : IDistributedEventHandler<NFTInfoExtraEto>, ITransientDependency
{
    private readonly INESTRepository<NFTInfoExtensionIndex, string> _nftInfoExtensionRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<NFTInfoExtensionHandler> _logger;

    public NFTInfoExtensionHandler(
        INESTRepository<NFTInfoExtensionIndex, string> nftInfoExtensionRepository,
        IObjectMapper objectMapper,
        ILogger<NFTInfoExtensionHandler> logger)
    {
        _nftInfoExtensionRepository = nftInfoExtensionRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    [ExceptionHandler(typeof(Exception),
        Message = "NFTInfoExtensionHandler.HandleEventAsync nftInfoExtension information add or update fail", 
        LogOnly = true,
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new []{"eventData" }
    )]
    public virtual async Task HandleEventAsync(NFTInfoExtraEto eventData)
    {
        var infoExtensionIndex = _objectMapper.Map<NFTInfoExtraEto, NFTInfoExtensionIndex>(eventData);

        await _nftInfoExtensionRepository.AddOrUpdateAsync(infoExtensionIndex);

        if (infoExtensionIndex != null)
        {
            _logger.LogDebug("nftInfoExtension information add or update success: {userInformation}",
                JsonConvert.SerializeObject(infoExtensionIndex));
        }
    }
}