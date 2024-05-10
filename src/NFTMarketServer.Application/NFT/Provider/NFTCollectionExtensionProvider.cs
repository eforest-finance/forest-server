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

    private const string OwnerTotalbyday = "OwnerTotalbyday";
    private const string OwnerTotalbyweek = "OwnerTotalbyweek";
    
    private static readonly Dictionary<string, Expression<Func<NFTCollectionExtensionIndex, object>>>
        SortingExpressions =
            new Dictionary<string, Expression<Func<NFTCollectionExtensionIndex, object>>>(StringComparer
                .OrdinalIgnoreCase)
            {
                { "FloorPricebyday", p => p.FloorPrice },
                { "ItemTotalbyday", p => p.ItemTotal },
                { OwnerTotalbyday, p => p.OwnerTotal },
                { "FloorPricebyweek", p => p.FloorPrice },
                { "ItemTotalbyweek", p => p.ItemTotal },
                { OwnerTotalbyweek, p => p.OwnerTotal },
                { "volumeTotalbyday", p => p.CurrentDayVolumeTotal },
                { "volumeTotalbyweek", p => p.CurrentWeekVolumeTotal },
                { "volumeTotalChangebyday", p => p.CurrentDayVolumeTotalChange },
                { "volumeTotalChangebyweek", p => p.CurrentWeekVolumeTotalChange },
                { "floorChangebyday", p => p.CurrentDayFloorChange },
                { "floorChangebyweek", p => p.CurrentWeekFloorChange },
                { "salesTotalbyday", p => p.CurrentDaySalesTotal },
                { "salesTotalbyweek", p => p.CurrentWeekSalesTotal },
                { "SupplyTotalbyday", p => p.SupplyTotal },
                { "SupplyTotalbyweek", p => p.SupplyTotal },
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
        var tuple = await _nftCollectionExtensionIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: GetSortingExpression(input.Sort + input.DateRangeType, input.SortType));

        return tuple;
    }
    
    public async Task<Tuple<long, List<NFTCollectionExtensionIndex>>> GetNFTCollectionExtensionAsync(
        SearchCollectionsFloorPriceInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionExtensionIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(t => t.Field(f => f.NFTSymbol).Terms(input.CollectionSymbolList)));
        mustQuery.Add(q => q.Term(t => t.Field(f => f.ChainId).Value(input.ChainId)));
        
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTCollectionExtensionIndex>, QueryContainer>>();
        
        var hideCollectionInfo = _hideCollectionInfoOptionsMonitor?.CurrentValue;
        if (hideCollectionInfo != null && !hideCollectionInfo.HideCollectionInfoList.IsNullOrEmpty())
        {
            mustNotQuery.Add(q => q.Terms(t => t.Field(f => f.NFTSymbol).Terms(hideCollectionInfo.HideCollectionInfoList)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionExtensionIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        
        var tuple = await _nftCollectionExtensionIndexRepository.GetListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount);

        return tuple;
    }

    public async Task<Tuple<long, List<NFTCollectionExtensionIndex>>> GetNFTCollectionExtensionPageAsync(int skipCount,
        int limit)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTCollectionExtensionIndex>, QueryContainer>>();

        QueryContainer Filter(QueryContainerDescriptor<NFTCollectionExtensionIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<NFTCollectionExtensionIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.CurrentDayVolumeTotal).Descending(t => t.CurrentWeekVolumeTotal)
        );

        var tuple = await _nftCollectionExtensionIndexRepository.GetSortListAsync(Filter, skip: skipCount, limit: limit,
            sortFunc: sorting);
        return tuple;
    }

    private Func<SortDescriptor<NFTCollectionExtensionIndex>, IPromise<IList<ISort>>> GetSortingExpression(string sortBy,SortOrder sortOrder)
    {
        var sortDescriptor = new SortDescriptor<NFTCollectionExtensionIndex>();
        if (sortBy.IsNullOrEmpty())
        {
            sortDescriptor.Descending(a => a.CreateTime);
            sortDescriptor.Descending(a => a.OwnerTotal);
            return s => sortDescriptor;;
        }

        if (SortingExpressions.TryGetValue(sortBy, out var expression))
        {
            if (sortOrder == SortOrder.Descending)
            {
                sortDescriptor.Descending(expression);
            }
            else
            {
                sortDescriptor.Ascending(expression);
            }

        }
        else
        {
            sortDescriptor.Descending(a => a.CreateTime);
        }
        
        if (!sortBy.Equals(OwnerTotalbyday) && !sortBy.Equals(OwnerTotalbyweek))
        {
            sortDescriptor.Descending(a => a.OwnerTotal);
        }
        
        return s => sortDescriptor;
    }
}