using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;

namespace NFTMarketServer.NFT;

public class NFTCollectionChangeService : NFTMarketServerAppService, INFTCollectionChangeService
{
    private readonly ILogger<NFTCollectionChangeService> _logger;
    private readonly INFTCollectionProvider _nftCollectionProvider;
    private readonly INFTCollectionProviderAdapter _nftCollectionProviderAdapter;
    public NFTCollectionChangeService(ILogger<NFTCollectionChangeService> logger,
        INFTCollectionProvider nftCollectionProvider,
        INFTCollectionProviderAdapter nftCollectionProviderAdapter)
    {
        _logger = logger;
        _nftCollectionProvider = nftCollectionProvider;
        _nftCollectionProviderAdapter = nftCollectionProviderAdapter;
    }

    public async Task<long> HandleItemsChangesAsync(string chainId, List<IndexerNFTCollectionChange> collectionChanges)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();
        try
        {
            foreach (var collectionChange in collectionChanges)
            {
                //mark maxProcessedBlockHeight
                blockHeight = Math.Max(blockHeight, collectionChange.BlockHeight);
                stopwatch.Start();
                var collectionId =
                    IdGenerateHelper.GetNFTCollectionId(collectionChange.ChainId, collectionChange.Symbol);
                var nftCollectionExtension =
                    await _nftCollectionProvider.GenerateNFTCollectionExtensionById(chainId, collectionChange.Symbol);
                stopwatch.Stop();
                _logger.LogInformation(
                    "It took {Elapsed} ms to execute GetNFTCollectionExtensionByIdAsync for NFT Collection chainId:{chainId} symbol: {symbol}, collection extension data: {data}.",
                    stopwatch.ElapsedMilliseconds, chainId, collectionChange.Symbol,
                    JsonConvert.SerializeObject(nftCollectionExtension));
                var dto = new NFTCollectionExtensionDto
                {
                    Id = collectionId,
                    ItemTotal = nftCollectionExtension.ItemTotal,
                    OwnerTotal = nftCollectionExtension.OwnerTotal
                };
                await _nftCollectionProviderAdapter.AddOrUpdateNftCollectionExtensionAsync(dto);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HandleItemsChangesAsync failed collectionChanges {collectionChanges}",
                JsonConvert.SerializeObject(collectionChanges));
        }

        return blockHeight;
    }

    public async Task<long> HandlePriceChangesAsync(string chainId,
        List<IndexerNFTCollectionPriceChange> collectionChanges)
    {
        long blockHeight = -1;
        var stopwatch = new Stopwatch();

        try
        {
            foreach (var collectionChange in collectionChanges)
            {
                blockHeight = Math.Max(blockHeight, collectionChange.BlockHeight);
                stopwatch.Start();
                var collectionId =
                    IdGenerateHelper.GetNFTCollectionId(collectionChange.ChainId, collectionChange.Symbol);
                var collectionPrice =
                    await _nftCollectionProvider.GetNFTCollectionPriceAsync(chainId, collectionChange.Symbol, -1);
                stopwatch.Stop();
                _logger.LogInformation(
                    "It took {Elapsed} ms to execute GetNFTCollectionPriceAsync for NFT Collection ChainId:{chainId} Id: {Id} floorPrice: {data}.",
                    stopwatch.ElapsedMilliseconds, chainId, collectionId, collectionPrice.floorPrice);
                var dto = new NFTCollectionExtensionDto
                {
                    Id = collectionId,
                    FloorPrice = collectionPrice.floorPrice
                };
                await _nftCollectionProviderAdapter.AddOrUpdateNftCollectionExtensionAsync(dto);
            }
        }
        catch (Exception e)
        {
            _logger.LogError(e, "HandlePriceChangesAsync failed collectionChanges {collectionChanges}",
                JsonConvert.SerializeObject(collectionChanges));
        }

        return blockHeight;
    }
}