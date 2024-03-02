using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Basic;
using NFTMarketServer.Entities;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTTraitProvider
{
    public Task CheckAndUpdateTraitInfo(NFTInfoNewIndex nftInfoNewIndex);
}

public class NFTTraitProvider : INFTTraitProvider
{
    private readonly INESTRepository<NFTCollectionTraitKeyIndex, string> _nftCollectionTraitKeyIndexRepository;
    private readonly INESTRepository<NFTCollectionTraitPairsIndex, string> _nftCollectionTraitPairsIndexRepository;

    private readonly INESTRepository<NFTCollectionTraitGenerationIndex, string>
        _nftCollectionTraitGenerationIndexRepository;

    private readonly ILogger<NFTTraitProvider> _logger;
    private readonly INFTInfoAppService _nftInfoAppService;

    public NFTTraitProvider(
        ILogger<NFTTraitProvider> logger,
        INESTRepository<NFTCollectionTraitKeyIndex, string> nftCollectionTraitKeyIndexRepository,
        INESTRepository<NFTCollectionTraitPairsIndex, string> nftCollectionTraitPairsIndexRepository,
        INESTRepository<NFTCollectionTraitGenerationIndex, string> nftCollectionTraitGenerationIndexRepository,
        INFTInfoAppService nftInfoAppService
    )
    {
        _logger = logger;
        _nftCollectionTraitKeyIndexRepository = nftCollectionTraitKeyIndexRepository;
        _nftCollectionTraitPairsIndexRepository = nftCollectionTraitPairsIndexRepository;
        _nftCollectionTraitGenerationIndexRepository = nftCollectionTraitGenerationIndexRepository;
        _nftInfoAppService = nftInfoAppService;
    }

    public async Task CheckAndUpdateTraitInfo(NFTInfoNewIndex nftInfoNewIndex)
    {
        if (nftInfoNewIndex == null)
        {
            return;
        }

        if (nftInfoNewIndex.TraitPairsDictionary.IsNullOrEmpty())
        {
            return;
        }

        foreach (var item in nftInfoNewIndex.TraitPairsDictionary)
        {
            if (item == null || string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Value))
            {
                continue;
            }

            await CheckAndUpdateNFTCollectionTraitKeyIndexInfo(nftInfoNewIndex, item);
            await CheckAndUpdateNFTCollectionTraitPairsIndexInfo(nftInfoNewIndex, item);
            await CheckAndUpdateNFTCollectionTraitGenerationIndexInfo(nftInfoNewIndex, item);
        }
    }

    private async Task<NFTCollectionTraitKeyIndex> QueryNFTCollectionTraitKeyIndexById(string id)
    {
        return await _nftCollectionTraitKeyIndexRepository.GetAsync(id);
    }

    private async Task<NFTCollectionTraitPairsIndex> QueryNFTCollectionTraitPairsIndexById(string id)
    {
        return await _nftCollectionTraitPairsIndexRepository.GetAsync(id);
    }

    public async Task<NFTCollectionTraitGenerationIndex> QueryNFTCollectionTraitGenerationIndexById(string id)
    {
        return await _nftCollectionTraitGenerationIndexRepository.GetAsync(id);
    }

    private async Task CheckAndUpdateNFTCollectionTraitKeyIndexInfo(NFTInfoNewIndex nftInfoNewIndex,
        ExternalInfoDictionary trait)
    {
        var id = IdGenerateHelper.GetNFTCollectionTraitKeyId(nftInfoNewIndex.CollectionSymbol, trait.Key);
        var nftCollectionTraitKeyIndex = await QueryNFTCollectionTraitKeyIndexById(id);

        if (nftCollectionTraitKeyIndex == null)
        {
            nftCollectionTraitKeyIndex = new NFTCollectionTraitKeyIndex
            {
                Id = id,
                NFTCollectionSymbol = nftInfoNewIndex.CollectionSymbol,
                TraitKey = trait.Key,
                ItemCount = nftInfoNewIndex.Supply > CommonConstant.IntZero
                    ? CommonConstant.LongOne
                    : CommonConstant.IntZero,
            };
        }

        var newCount = await _nftInfoAppService.QueryItemCountForNFTCollectionWithTraitKeyAsync(trait.Key,
            nftInfoNewIndex.CollectionId);
        if (nftCollectionTraitKeyIndex.ItemCount == newCount)
        {
            return;
        }

        nftCollectionTraitKeyIndex.ItemCount = newCount;

        await _nftCollectionTraitKeyIndexRepository.AddOrUpdateAsync(nftCollectionTraitKeyIndex);
    }

    private async Task CheckAndUpdateNFTCollectionTraitPairsIndexInfo(NFTInfoNewIndex nftInfoNewIndex,
        ExternalInfoDictionary trait)
    {
        var id = IdGenerateHelper.GetNFTCollectionTraitPairsId(nftInfoNewIndex.CollectionSymbol, trait.Key,
            trait.Value);
        var nftCollectionTraitPairsIndex = await QueryNFTCollectionTraitPairsIndexById(id);
        if (nftCollectionTraitPairsIndex == null)
        {
            nftCollectionTraitPairsIndex = new NFTCollectionTraitPairsIndex
            {
                Id = id,
                NFTCollectionSymbol = nftInfoNewIndex.CollectionSymbol,
                TraitKey = trait.Key,
                TraitValue = trait.Value,
                ItemCount = nftInfoNewIndex.Supply > CommonConstant.IntZero
                    ? CommonConstant.LongOne
                    : CommonConstant.IntZero,
                FloorPriceSymbol = CommonConstant.Coin_ELF,
                FloorPriceToken = null,
                FloorPriceNFTSymbol = ""
            };
        }

        bool changeFlag = false;
        var newItemCount = await _nftInfoAppService.QueryItemCountForNFTCollectionWithTraitPairAsync(trait.Key,
            trait.Value,
            nftInfoNewIndex.CollectionId);
        if (nftCollectionTraitPairsIndex.ItemCount != newItemCount)
        {
            changeFlag = true;
            nftCollectionTraitPairsIndex.ItemCount = newItemCount;

        }

        var floorPriceNFT = await _nftInfoAppService.QueryFloorPriceNFTForNFTWithTraitPair(trait.Key,
            trait.Value,
            nftInfoNewIndex.CollectionId);
        if (floorPriceNFT?.ListingPrice != nftInfoNewIndex.ListingPrice)
        {
            changeFlag = true;
            nftInfoNewIndex.ListingPrice = (decimal)floorPriceNFT?.ListingPrice;
            nftInfoNewIndex.ListingEndTime = (DateTime)floorPriceNFT?.ListingEndTime;
            nftInfoNewIndex.ListingToken = floorPriceNFT?.ListingToken;
        }

        if (changeFlag)
        {
            await _nftCollectionTraitPairsIndexRepository.AddOrUpdateAsync(nftCollectionTraitPairsIndex);
        }
    }

    private async Task CheckAndUpdateNFTCollectionTraitGenerationIndexInfo(NFTInfoNewIndex nftInfoNewIndex,
        ExternalInfoDictionary trait)
    {
        var id = IdGenerateHelper.GetNFTCollectionTraitGenerationId(nftInfoNewIndex.CollectionSymbol);
        var nftCollectionTraitGenerationIndex = await QueryNFTCollectionTraitGenerationIndexById(id);
        if (nftCollectionTraitGenerationIndex == null)
        {
            nftCollectionTraitGenerationIndex = new NFTCollectionTraitGenerationIndex()
            {
                Id = id,
                ItemCount = nftInfoNewIndex.Supply > CommonConstant.IntZero
                    ? CommonConstant.LongOne
                    : CommonConstant.IntZero,
                Generation = nftInfoNewIndex.Generation
            };
        }

        var newCount = await _nftInfoAppService.QueryItemCountForNFTCollectionGenerationAsync(
            nftInfoNewIndex.CollectionId, nftInfoNewIndex.Generation);
        if (nftCollectionTraitGenerationIndex.ItemCount == newCount)
        {
            return;
        }
        
        nftCollectionTraitGenerationIndex.ItemCount = newCount;
        
        await _nftCollectionTraitGenerationIndexRepository.AddOrUpdateAsync(nftCollectionTraitGenerationIndex);
    }
}