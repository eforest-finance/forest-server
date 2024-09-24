using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AutoMapper.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Options;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface ICompositeNFTProvider
{
    public Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoAsync(List<string> collectionIdList,
        string searchName, int skipCount, int maxResultCount);
    public Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoAsync(List<string> nftInfoIdList);
}

public class CompositeNFTProvider : ICompositeNFTProvider, ISingletonDependency
{
    private readonly ILogger<CompositeNFTProvider> _logger;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IOptionsMonitor<FuzzySearchOptions>
        _fuzzySearchOptionsMonitor;
    public CompositeNFTProvider(
        ILogger<CompositeNFTProvider> logger,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IOptionsMonitor<FuzzySearchOptions> fuzzySearchOptionsMonitor
    )
    {
        _logger = logger;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _fuzzySearchOptionsMonitor = fuzzySearchOptionsMonitor;

    }

    public async Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoAsync(List<string> collectionIdList,
        string searchName, int skipCount, int maxResultCount)
    {
        var fuzzySearchSwitch = _fuzzySearchOptionsMonitor.CurrentValue.FuzzySearchSwitch;
        var commonNFTInfos =
            await QueryCompositeNFTInfoForCommonNFTAsync(collectionIdList, new List<string>(), searchName, skipCount,
                maxResultCount,false, fuzzySearchSwitch);
        _logger.LogInformation("QueryCompositeNFTInfoAsync commonNFTInfos{A}", commonNFTInfos.Count);
        var seedInfos =
            await QueryCompositeNFTInfoForSeedAsync(searchName, new List<string>(), skipCount, maxResultCount, fuzzySearchSwitch);
        _logger.LogInformation("QueryCompositeNFTInfoAsync seedInfos{A}", seedInfos.Count);

        var mergedDict = commonNFTInfos.Concat(seedInfos).ToDictionary(pair => pair.Key, pair => pair.Value);

        return mergedDict;
    }

    public async Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoAsync(List<string> nftInfoIdList)
    {
        if (nftInfoIdList.IsNullOrEmpty())
        {
            return new Dictionary<string, CompositeNFTDto>();
        }

        var maxResultCount = nftInfoIdList.Count;
        var commonNFTInfos =
            await QueryCompositeNFTInfoForCommonNFTAsync(new List<string>(), nftInfoIdList, string.Empty,
                CommonConstant.IntZero, maxResultCount,false
            );

        var seedInfos =
            await QueryCompositeNFTInfoForSeedAsync(string.Empty, nftInfoIdList, CommonConstant.IntZero,
                maxResultCount);

        var mergedDict = commonNFTInfos.Concat(seedInfos).ToDictionary(pair => pair.Key, pair => pair.Value);

        return mergedDict;
    }

    private async Task<Dictionary<string, CompositeNFTDto>> QueryCompositeNFTInfoForCommonNFTAsync(
        List<string> collectionIdList, List<string> nftInfoIdList, string
            searchName, int skipCount, int maxResultCount, bool countFlagIsTrue, bool fuzzySearchSwitch = false)
    {
        if (collectionIdList.IsNullOrEmpty() && searchName.IsNullOrEmpty() && nftInfoIdList.IsNullOrEmpty())
        {
            return new Dictionary<string, CompositeNFTDto>();
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
        
        if (!collectionIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.CollectionId).Terms(collectionIdList)));
        }
        
        if (!nftInfoIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(nftInfoIdList)));
        }

        if (!searchName.IsNullOrEmpty() && !fuzzySearchSwitch)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenName).Value(searchName)));
        }
        if (!searchName.IsNullOrEmpty() && fuzzySearchSwitch)
        {
            mustQuery.Add(q => q.Wildcard(i => i.Field(f => f.FuzzyTokenName).Value("*" + searchName + "*").CaseInsensitive(true)));
        }

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
        searchName, List<string> nftInfoIdList, int skipCount, int maxResultCount, bool fuzzySearchSwitch = false)
    {
        if (searchName.IsNullOrEmpty() && nftInfoIdList.IsNullOrEmpty())
        {
            return new Dictionary<string, CompositeNFTDto>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        if (!searchName.IsNullOrEmpty() && !fuzzySearchSwitch)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenName).Value(searchName)));
        }
        if (!searchName.IsNullOrEmpty() && fuzzySearchSwitch)
        {
            mustQuery.Add(q => q.Wildcard(i => i.Field(f => f.FuzzyTokenName).Value("*" + searchName + "*").CaseInsensitive(true)));
        }
        if (!nftInfoIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(nftInfoIdList)));
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