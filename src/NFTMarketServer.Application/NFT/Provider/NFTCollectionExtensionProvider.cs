using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public class NFTCollectionExtensionProvider : INFTCollectionExtensionProvider, ISingletonDependency
{
    
    private readonly IOptionsMonitor<HideCollectionInfoOptions> _hideCollectionInfoOptionsMonitor;
    
    private static readonly Dictionary<string, Expression<Func<NFTCollectionExtensionIndex, object>>>
        SortingExpressions =
            new Dictionary<string, Expression<Func<NFTCollectionExtensionIndex, object>>>(StringComparer
                .OrdinalIgnoreCase)
            {
                { "FloorPrice", p => p.FloorPrice },
                { "ItemTotal", p => p.ItemTotal },
                { "OwnerTotal", p => p.OwnerTotal }
            };
    private readonly INESTRepository<NFTCollectionExtensionIndex, string> _nftCollectionExtensionIndexRepository;
    public NFTCollectionExtensionProvider(
        IOptionsMonitor<HideCollectionInfoOptions> hideCollectionInfoOptionsMonitor,
        INESTRepository<NFTCollectionExtensionIndex, string> nftCollectionExtensionIndexRepository)
    {
        _nftCollectionExtensionIndexRepository = nftCollectionExtensionIndexRepository;
        _hideCollectionInfoOptionsMonitor = hideCollectionInfoOptionsMonitor;
    }
    
    public async Task<Dictionary<string, NFTCollectionExtensionIndex>> GetNFTCollectionExtensionsAsync(
        List<string> nftCollectionExtensionIndexIds)
    {
        var result = new Dictionary<string, NFTCollectionExtensionIndex>();
        if (nftCollectionExtensionIndexIds == null)
        {
            return result;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionExtensionIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id)
            .Terms(nftCollectionExtensionIndexIds)));

        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionExtensionIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var extensions =
            await _nftCollectionExtensionIndexRepository.GetListAsync(Filter);
        if (extensions == null || extensions.Item2 == null)
        {
            return result;
        }

        foreach (NFTCollectionExtensionIndex tem in extensions.Item2)
        {
            result.Add(tem.Id, tem);
        }

        return result;
    }

    public async Task<NFTCollectionExtensionIndex> GetNFTCollectionExtensionAsync(string nftCollectionExtensionIndexId)
    {
        if (nftCollectionExtensionIndexId.IsNullOrEmpty())
        {
            return null;
        }

        var result = GetNFTCollectionExtensionsAsync(new List<String>() { nftCollectionExtensionIndexId });
        if (result == null || result.Result == null) return null;
        var dictionary = result.Result;
        if (!dictionary.ContainsKey(nftCollectionExtensionIndexId))
        {
            return null;
        }

        return dictionary[nftCollectionExtensionIndexId];
    }

    public async Task<Tuple<long, List<NFTCollectionExtensionIndex>>> GetNFTCollectionExtensionAsync(
        SearchNFTCollectionsInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionExtensionIndex>, QueryContainer>>();

        if (!input.TokenName.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.TokenName).Value(input.TokenName)));
        }

        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTCollectionExtensionIndex>, QueryContainer>>();
        
        var hideCollectionInfo = _hideCollectionInfoOptionsMonitor?.CurrentValue;
        if (hideCollectionInfo != null && !hideCollectionInfo.HideCollectionInfoList.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Terms(t => t.Field(f => f.NFTSymbol).Terms(hideCollectionInfo.HideCollectionInfoList)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionExtensionIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));

        //sortExp base Sort , like "floorPrice", "itemTotal"
        var tuple = await _nftCollectionExtensionIndexRepository.GetListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortType: input.SortType,
            sortExp: GetSortingExpression(input.Sort));

        return tuple;
    }
    private Expression<Func<NFTCollectionExtensionIndex, object>> GetSortingExpression(string sortBy)
    {
        if (sortBy.IsNullOrEmpty())
        {
            return o => o.CreateTime;
        }
        if (SortingExpressions.TryGetValue(sortBy, out var expression))
        {
            return expression;
        }
        return p => p.CreateTime;
    }
}