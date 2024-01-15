using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Options;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT.Provider;

public interface INFTCollectionProviderAdapter
{
    Task AddOrUpdateNftCollectionExtensionAsync(NFTCollectionExtensionDto dto);
}

public class NFTCollectionProviderAdapter : INFTCollectionProviderAdapter, ISingletonDependency
{
    private readonly ILogger<NFTCollectionExtensionProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INFTCollectionProvider _nftCollectionProvider;
    private readonly IOptionsMonitor<NFTImageUrlOptions> _nftImageUrlOptionsMonitor;

    public NFTCollectionProviderAdapter(ILogger<NFTCollectionExtensionProvider> logger, 
        IObjectMapper objectMapper, 
        IClusterClient clusterClient, 
        IDistributedEventBus distributedEventBus, 
        INFTCollectionProvider nftCollectionProvider,
        IOptionsMonitor<NFTImageUrlOptions> nftImageUrlOptionsMonitor)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _nftCollectionProvider = nftCollectionProvider;
        _nftImageUrlOptionsMonitor = nftImageUrlOptionsMonitor;
    }

    public async Task AddOrUpdateNftCollectionExtensionAsync(NFTCollectionExtensionDto dto)
    {
        var nftCollectionExtensionGrain = _clusterClient.GetGrain<INFTCollectionExtensionGrain>(dto.Id);
        var grainDto = (await nftCollectionExtensionGrain.GetAsync()).Data;
        //when grainDto or it's id is empty return.
        if (grainDto.Id.IsNullOrEmpty())
        {
            _logger.LogInformation("NftCollectionExtensionGrain start create {id}.", dto.Id);
            var collection = await _nftCollectionProvider.GetNFTCollectionIndexAsync(dto.Id);
            AssertHelper.IsTrue(collection != null && !collection.Id.IsNullOrEmpty(), "Collection {id} is empty.", dto.Id);
            var collectionExtension = _objectMapper.Map<IndexerNFTCollection, NftCollectionExtensionGrainDto>(collection);
            collectionExtension.LogoImage ??= NFTHelper.GetNftImageUrl(collection.ExternalInfoDictionary, 
                () => _nftImageUrlOptionsMonitor.CurrentValue.GetImageUrl(collection.Symbol));
            var createResultDto = await nftCollectionExtensionGrain.CreateNftCollectionExtensionAsync(collectionExtension);
            if (!createResultDto.Success)
            {
                _logger.LogError("CreateCollectionExtension  fail, CollectionExtension id: {id}.", dto.Id);
                return;
            }
            grainDto = createResultDto.Data;
        }
        grainDto.FloorPriceSymbol = SymbolHelper.CoinGeckoELF();
        grainDto.OfCollectionExtensionDto(dto);
        var resultDto = await nftCollectionExtensionGrain.UpdateNftCollectionExtensionAsync(grainDto);
        if (!resultDto.Success)
        {
            _logger.LogError("SaveCollectionExtension fail, CollectionExtension id: {id}.", grainDto.Id);
            return;
        }

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<NftCollectionExtensionGrainDto, NFTCollectionExtraEto>(resultDto.Data));
    }
}