using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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

    public async Task HandleEventAsync(NFTInfoExtraEto eventData)
    {
        try
        {
            var infoExtensionIndex = _objectMapper.Map<NFTInfoExtraEto, NFTInfoExtensionIndex>(eventData);

            await _nftInfoExtensionRepository.AddOrUpdateAsync(infoExtensionIndex);

            if (infoExtensionIndex != null)
            {
                _logger.LogDebug("nftInfoExtension information add or update success: {userInformation}",
                    JsonConvert.SerializeObject(infoExtensionIndex));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "nftInfoExtension information add or update fail: {Data}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}