using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AutoMapper.Internal;
using AutoMapper.Internal.Mappers;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface ICompositeNFTProvider
{
    public Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoAsync(List<string> collectionIdList,
        string searchName, int skipCount, int maxResultCount);
}

public class CompositeNFTProvider : ICompositeNFTProvider, ISingletonDependency
{
    private readonly ILogger<CompositeNFTProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly INESTRepository<NFTCollectionExtensionIndex, string> _nftCollectionExtensionIndexRepository;

    public CompositeNFTProvider(
        ILogger<CompositeNFTProvider> logger,
        IObjectMapper objectMapper,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        INESTRepository<NFTCollectionExtensionIndex, string> nftCollectionExtensionIndexRepository
    )
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _nftCollectionExtensionIndexRepository = nftCollectionExtensionIndexRepository;
    }

    public async Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoAsync(List<string> collectionIdList,
        string searchName, int skipCount, int maxResultCount)
    {
        var commonNFTInfos =
            await QueryCompositeNFTInfoForCommonNFTAsync(collectionIdList, searchName, skipCount, maxResultCount);

        var seedInfos =
            await QueryCompositeNFTInfoForSeedAsync(searchName, skipCount, maxResultCount);

        var mergedDict = commonNFTInfos.Concat(seedInfos).ToDictionary(pair => pair.Key, pair => pair.Value);

        return mergedDict;
    }
    
    private async Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoForCommonNFTAsync(List<string> collectionIdList, string
        searchName, int skipCount, int maxResultCount)
    {
        if (collectionIdList.IsNullOrEmpty() && searchName.IsNullOrEmpty())
        {
            return new Dictionary<string, CompositeNFTDto>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
        
        if (!collectionIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CollectionId).Terms(collectionIdList)));
        }

        if (!searchName.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenName).Value(searchName)));
        }
        
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.CountedFlag).Value(true)));

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, skip: skipCount,
            limit: maxResultCount);
        if (result== null || result.Item1 == CommonConstant.IntZero)
        {
            return new Dictionary<string, CompositeNFTDto>();
        }

        return result.Item2?.ToDictionary(i => i.Id, i => BuildCompositeNFTDto(i));
    }

    private static CompositeNFTDto BuildCompositeNFTDto(NFTInfoNewIndex i)
    {
        return new CompositeNFTDto()
        {
            ChainId = i.ChainId,
            CollectionId = i.CollectionId,
            CollectionName = i.CollectionName,
            CollectionSymbol = i.CollectionSymbol,
            Decimals = i.Decimals,
            NFTInfoId = i.Id,
            NFTName = i.TokenName,
            NFTType = NFTType.NFT,
            PreviewImage = SymbolHelper.BuildNFTImage(i),
            Symbol = i.Symbol
        };
    }
    
    private async Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoForSeedAsync(string
        searchName, int skipCount, int maxResultCount)
    {
        if (searchName.IsNullOrEmpty())
        {
            return new Dictionary<string, CompositeNFTDto>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        if (!searchName.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenName).Value(searchName)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _seedSymbolIndexRepository.GetSortListAsync(Filter, skip: skipCount,
            limit: maxResultCount);
        if (result== null || result.Item1 == CommonConstant.IntZero)
        {
            return new Dictionary<string, CompositeNFTDto>();
        }

        return result.Item2?.ToDictionary(i => i.Id, i => BuildCompositeNFTDto(i));
    }
    
    private static CompositeNFTDto BuildCompositeNFTDto(SeedSymbolIndex i)
    {
        var collectionId = SymbolHelper.TransferNFTIdToCollectionId(i.Id);
        return new CompositeNFTDto()
        {
            ChainId = i.ChainId,
            CollectionId = collectionId,
            CollectionName = CommonConstant.CollectionSeedName,
            CollectionSymbol = IdGenerateHelper.GetCollectionIdSymbol(collectionId),
            Decimals = i.Decimals,
            NFTInfoId = i.Id,
            NFTName = i.TokenName,
            NFTType = NFTType.NFT,
            PreviewImage = i.SeedImage,
            Symbol = i.Symbol
        };
    }
    
    
}