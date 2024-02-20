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
    
    public async Task HandleEventAsync(NFTDropExtraEto eventData)
    {
        try
        {
            var nftDropExtensionIndex = _objectMapper.Map<NFTDropExtraEto, NFTDropExtensionIndex>(eventData);

            await _nftDropExtensionRepository.AddOrUpdateAsync(nftDropExtensionIndex);

            if (nftDropExtensionIndex != null)
            {
                _logger.LogDebug("nftInfoExtension information add or update success: {userInformation}",
                    JsonConvert.SerializeObject(nftDropExtensionIndex));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "nftInfoExtension information add or update fail: {Data}",
                JsonConvert.SerializeObject(eventData));
        }
    }
}