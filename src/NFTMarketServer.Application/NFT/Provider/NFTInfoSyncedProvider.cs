using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT.Provider;


public interface INFTInfoSyncedProvider
{
    public Task<IndexerNFTInfo> GetNFTInfoIndexAsync(string id);
    
    public Task<Tuple<long, List<IndexerNFTInfo>>> GetNFTBriefInfosAsync(GetCompositeNFTInfosInput dto);
    
    public Task<IndexerNFTInfos> GetNFTInfosUserProfileAsync(GetNFTInfosProfileInput dto);
}

public class NFTInfoSyncedProvider : INFTInfoSyncedProvider, ISingletonDependency
{
    private readonly ILogger<SeedSymbolSyncedProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<NFTInfoIndex, string> _nftInfoIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IUserBalanceProvider _userBalanceProvider;
    
    
    public NFTInfoSyncedProvider(ILogger<SeedSymbolSyncedProvider> logger, IObjectMapper objectMapper, 
        INESTRepository<NFTInfoIndex, string> nftInfoIndexRepository, 
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository, 
        IUserBalanceProvider userBalanceProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _userBalanceProvider = userBalanceProvider;
    }

    public async Task<IndexerNFTInfo> GetNFTInfoIndexAsync(string nftInfoId)
    {
        var isSeed = nftInfoId.Match(NFTSymbolBasicConstants.SeedIdPattern);
        var res = isSeed
            ? _objectMapper.Map<SeedSymbolIndex, IndexerNFTInfo>(await _seedSymbolIndexRepository.GetAsync(nftInfoId))
            : _objectMapper.Map<NFTInfoIndex, IndexerNFTInfo>(await _nftInfoIndexRepository.GetAsync(nftInfoId));
        if (res == null)
        {
            return null;
        }

        if (isSeed)
        {
            res.SeedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValue(res.ExternalInfoDictionary,
                TokenCreatedExternalInfoEnum.SeedOwnedSymbol, res.TokenName);
        }
        var balanceInfo = await _userBalanceProvider.GetNFTBalanceInfoAsync(nftInfoId);
        res.Owner = balanceInfo.Owner;
        res.AllOwnerCount = balanceInfo.OwnerCount;
        return res;
    }

    public async Task<Tuple<long, List<IndexerNFTInfo>>> GetNFTBriefInfosAsync(GetCompositeNFTInfosInput dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var shouldQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();

        mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(dto.CollectionId)));
        
        if (!dto.SearchParam.IsNullOrEmpty())
        {
            shouldQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(dto.SearchParam)));
            shouldQuery.Add(q => q.Term(i => i.Field(f => f.TokenName).Value(dto.SearchParam)));
        }

        if (!dto.ChainList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ChainId).Terms(dto.ChainList)));
        }
        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.Supply).GreaterThan(0)));

        if (!dto.HasListingFlag && !dto.HasOfferFlag)
        {
            AddQueryForMinListingPrice(mustQuery, dto);
        }
        if (dto.HasListingFlag)
        {
            AddQueryForMinListingPrice(mustQuery, dto);
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasListingFlag).Value(dto.HasListingFlag)))));
        }
        if (dto.HasOfferFlag)
        {
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasOfferFlag).Value(dto.HasOfferFlag)))));
        }
        
        if (shouldQuery.Any()){
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var sort = GetSortForNFTBrife(dto.Sorting);
        var result = await _nftInfoIndexRepository.GetSortListAsync(Filter, sortFunc: sort, skip: dto.SkipCount, limit: CommonConstant.IntMaxCount);
        if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
        {
            return  new Tuple<long, List<IndexerNFTInfo>>(result.Item1, _objectMapper.Map<List<NFTInfoIndex>, List<IndexerNFTInfo>>(result?.Item2));
        }
        
        var count = await QueryRealCountAsync(mustQuery);
        var newResult = new Tuple<long, List<IndexerNFTInfo>>(count, _objectMapper.Map<List<NFTInfoIndex>, List<IndexerNFTInfo>>(result?.Item2));
        return newResult;
    }

    public async Task<IndexerNFTInfos> GetNFTInfosUserProfileAsync(GetNFTInfosProfileInput dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>>();
        var sorting = new Tuple<SortOrder, Expression<Func<NFTInfoIndex, object>>>(SortOrder.Descending, o => o.LatestListingTime);
        var skipCount = dto.SkipCount;
        long? selfTotalCount = 0;
        if (dto.Status == NFTSymbolBasicConstants.NFTInfoQueryStatusSelf)
        {
            skipCount = 0;
            //query match nft
            var indexerUserMatchedNft = await _userBalanceProvider.GetUserMatchedNftIdsAsync(dto, false);
            selfTotalCount = indexerUserMatchedNft?.Count;
            if (indexerUserMatchedNft == null || indexerUserMatchedNft.NftIds.IsNullOrEmpty())
            {
                return new IndexerNFTInfos
                {
                    TotalRecordCount = selfTotalCount,
                    IndexerNftInfos = new List<IndexerNFTInfo>()
                };
            }
            mustQuery.Add(q => q.Ids(i => i.Values(indexerUserMatchedNft.NftIds)));
        }
        else
        {
            if (!dto.IssueAddress.IsNullOrEmpty())
            {
                mustQuery.Add(q => q.Terms(i => i.Field(f => f.IssueManagerSet).Terms(dto.IssueAddress)));
            }
        }

        if (dto.PriceLow != null && dto.PriceLow != 0)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.MinListingPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow))));
        }

        if (dto.PriceHigh != null && dto.PriceHigh != 0)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.MinListingPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))));
        }
        if (!dto.NFTInfoIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(dto.NFTInfoIds)));
        }

        if (!dto.NFTCollectionId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(dto.NFTCollectionId)));
        }
        //Exclude Burned All NFT ( supply = 0 and issued = totalSupply)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{NFTSymbolBasicConstants.BurnedAllNftScript}")
                )
            )
        );
        
        QueryContainer Filter(QueryContainerDescriptor<NFTInfoIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        }
        var result = await _nftInfoIndexRepository.GetListAsync(Filter, sortType: sorting.Item1, sortExp: sorting.Item2,
            skip: skipCount, limit: CommonConstant.IntMaxCount);
         var indexerInfos = new IndexerNFTInfos
        {
            TotalRecordCount = result.Item1,
            IndexerNftInfos = _objectMapper.Map<List<NFTInfoIndex>, List<IndexerNFTInfo>>(result.Item2)
        };
         if (dto.Status == NFTSymbolBasicConstants.NFTInfoQueryStatusSelf)
         {
             indexerInfos.TotalRecordCount = (long)selfTotalCount;
             return indexerInfos;
         }
         
         if (result?.Item1 != CommonConstant.EsLimitTotalNumber)
         {
             return indexerInfos;
         }
        
         var count = await QueryRealCountAsync(mustQuery);
         indexerInfos.TotalRecordCount = count;
         return indexerInfos;
    }

    private static void AddQueryForMinListingPrice(
        List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>> mustQuery,
        GetCompositeNFTInfosInput dto)
    {
        if (dto.PriceLow != null)
        {
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.MinListingPrice).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow))));
        }

        if (dto.PriceHigh != null)
        {
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.MinListingPrice).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))));
        }
    }
    
    private static Func<SortDescriptor<NFTInfoIndex>, IPromise<IList<ISort>>> GetSortForNFTBrife(string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            throw new NotSupportedException();
        }

        SortDescriptor<NFTInfoIndex> sortDescriptor = new SortDescriptor<NFTInfoIndex>();
        
        var sortingArray = sorting.Split(" ");
        
        switch (sortingArray[0])
        {
            case "Low":
                sortDescriptor.Descending(a => a.HasListingFlag);
                sortDescriptor.Ascending(a => a.MinListingPrice);
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            case "High":
                sortDescriptor.Descending(a => a.HasListingFlag);
                sortDescriptor.Descending(a => a.MinListingPrice);
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            case "Recently":
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            default:
                sortDescriptor.Descending(a => a.LatestListingTime);
                sortDescriptor.Descending(a => a.BlockHeight);
                break;
        }
        return s => sortDescriptor;
    }
    
    private async Task<long> QueryRealCountAsync(List<Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer>> mustQuery)
    {
        var countRequest = new SearchRequest<NFTInfoIndex>
        {
            Query = new BoolQuery
            {
                Must = mustQuery != null && mustQuery.Any()
                    ? mustQuery
                        .Select(func => func(new QueryContainerDescriptor<NFTInfoIndex>()))
                        .ToList()
                        .AsEnumerable()
                    : Enumerable.Empty<QueryContainer>()
            },
            Size = 0
        };
        
        Func<QueryContainerDescriptor<NFTInfoIndex>, QueryContainer> queryFunc = q => countRequest.Query;
        var realCount = await _nftInfoIndexRepository.CountAsync(queryFunc);
        return realCount.Count;
    }
}