using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.Entities;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Trait;

public interface ITraitInfoProvider
{
    public Task<Dictionary<string, NFTCollectionTraitKeyIndex>> QueryCollectionTraitKeyInfosAsync(
        string collectionSymbol, List<string> keyList);

    public Task<Dictionary<string, NFTCollectionTraitPairsIndex>> QueryCollectionTraitPairInfosAsync(
        string collectionSymbol, List<ExternalInfoDictionary> pairInfoDictionaries);

    public Task<List<string>> QueryTraitKeyListByCollectionSymbolAsync(
        string collectionSymbol, int skip, int size);

    public Task<Dictionary<string, List<NFTCollectionTraitPairsIndex>>> QueryCollectionTraitPairsInfoSortByKeyAsync(
        string collectionSymbol, List<string> keyList);

    public Task<Dictionary<int, long>> QueryCollectionGenerationInfoAsync(
        string collectionSymbol);
}

public class TraitInfoProvider : ITraitInfoProvider, ISingletonDependency
{
    private readonly INESTRepository<NFTCollectionTraitKeyIndex, string> _nftCollectionTraitKeyIndexRepository;
    private readonly INESTRepository<NFTCollectionTraitPairsIndex, string> _nftCollectionTraitPairsIndexRepository;

    private readonly INESTRepository<NFTCollectionTraitGenerationIndex, string>
        _nftCollectionTraitGenerationIndexRepository;

    public TraitInfoProvider(
        INESTRepository<NFTCollectionTraitKeyIndex, string> nftCollectionTraitKeyIndexRepository,
        INESTRepository<NFTCollectionTraitPairsIndex, string> nftCollectionTraitPairsIndexRepository,
        INESTRepository<NFTCollectionTraitGenerationIndex, string> nftCollectionTraitGenerationIndexRepository)
    {
        _nftCollectionTraitKeyIndexRepository = nftCollectionTraitKeyIndexRepository;
        _nftCollectionTraitPairsIndexRepository = nftCollectionTraitPairsIndexRepository;
        _nftCollectionTraitGenerationIndexRepository = nftCollectionTraitGenerationIndexRepository;
    }

    public async Task<Dictionary<string, NFTCollectionTraitKeyIndex>> QueryCollectionTraitKeyInfosAsync(
        string collectionId, List<string> keyList)
    {
        var ids = BuildCollectionTraitKeyIds(collectionId, keyList);
        if (ids.IsNullOrEmpty())
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionTraitKeyIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(ids)));

        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionTraitKeyIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftCollectionTraitKeyIndexRepository.GetSortListAsync(Filter);
        if (result?.Item1 == null || result?.Item1 == CommonConstant.EsLimitTotalNumber)
        {
            return null;
        }

        return result.Item2.ToDictionary(x => x.TraitKey, x => x);
    }

    public async Task<Dictionary<string, NFTCollectionTraitPairsIndex>> QueryCollectionTraitPairInfosAsync(
        string collectionSymbol, List<ExternalInfoDictionary> pairInfoDictionaries)
    {
        var ids = BuildCollectionTraitPairIds(collectionSymbol, pairInfoDictionaries);
        if (ids.IsNullOrEmpty())
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionTraitPairsIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(ids)));

        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionTraitPairsIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftCollectionTraitPairsIndexRepository.GetSortListAsync(Filter);
        if (result?.Item1 == null || result?.Item1 == CommonConstant.EsLimitTotalNumber)
        {
            return null;
        }

        return result.Item2.ToDictionary(x => x.TraitKey, x => x);
    }

    public async Task<List<string>> QueryTraitKeyListByCollectionSymbolAsync(string collectionSymbol
        , int skip, int size)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionTraitKeyIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.NFTCollectionSymbol).Value(collectionSymbol)));

        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionTraitKeyIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftCollectionTraitKeyIndexRepository.GetSortListAsync(Filter);
        if (result?.Item1 == null || result?.Item1 == CommonConstant.EsLimitTotalNumber)
        {
            return null;
        }

        return result.Item2.Select(x => x.TraitKey).ToList();
    }

    public async Task<Dictionary<string, List<NFTCollectionTraitPairsIndex>>>
        QueryCollectionTraitPairsInfoSortByKeyAsync(string collectionSymbol, List<string> keyList)
    {
        if (collectionSymbol.IsNullOrEmpty() || keyList.IsNullOrEmpty())
        {
            return new Dictionary<string, List<NFTCollectionTraitPairsIndex>>();
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionTraitPairsIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.NFTCollectionSymbol).Value(collectionSymbol)));

        mustQuery.Add(q => q.Terms(i => i.Field(f => f.TraitKey).Terms(keyList)));

        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionTraitPairsIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftCollectionTraitPairsIndexRepository.GetSortListAsync(Filter);
        if (result?.Item1 == null || result?.Item1 == CommonConstant.EsLimitTotalNumber)
        {
            return new Dictionary<string, List<NFTCollectionTraitPairsIndex>>();
        }

        return result.Item2
            .GroupBy(x => x.TraitKey)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    public async Task<Dictionary<int, long>> QueryCollectionGenerationInfoAsync(string collectionSymbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionTraitGenerationIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionSymbol).Value(collectionSymbol)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.Generation).GreaterThanOrEquals(CommonConstant.IntZero)));
        mustQuery.Add(q => q.Range(i => i.Field(f => f.ItemCount).GreaterThan(CommonConstant.IntOne)));
        
        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionTraitGenerationIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftCollectionTraitGenerationIndexRepository.GetListAsync(Filter,
            sortType: SortOrder.Ascending, sortExp: o => o.Generation);
        if (result?.Item1 == null || result?.Item1 == CommonConstant.EsLimitTotalNumber)
        {
            return new Dictionary<int, long>();
        }

        return result.Item2.ToDictionary(x => x.Generation, x => x.ItemCount);
    }

    private static List<string> BuildCollectionTraitKeyIds(string collectionSymbol, ICollection<string> keyList)
    {
        if (keyList.IsNullOrEmpty()) return new List<string>();
        return keyList.Select(x => IdGenerateHelper.GetNFTCollectionTraitKeyId(collectionSymbol, x)).ToList();
    }

    private static List<string> BuildCollectionTraitPairIds(string collectionSymbol,
        List<ExternalInfoDictionary> pairInfoDictionaries)
    {
        if (pairInfoDictionaries.IsNullOrEmpty()) return new List<string>();
        return pairInfoDictionaries
            .Select(x => IdGenerateHelper.GetNFTCollectionTraitPairsId(collectionSymbol, x.Key, x.Value)).ToList();
    }
}