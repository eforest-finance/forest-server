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


public interface INFTInfoNewSyncedProvider
{
    public Task<IndexerNFTInfo> GetNFTInfoIndexAsync(string id);
    
    public Task<Tuple<long, List<NFTInfoIndex>>> GetNFTBriefInfosAsync(GetCompositeNFTInfosInput dto);
    
    public Task<IndexerNFTInfos> GetNFTInfosUserProfileAsync(GetNFTInfosProfileInput dto);
}

public class NFTInfoNewSyncedProvider : INFTInfoNewSyncedProvider, ISingletonDependency
{
    private readonly ILogger<SeedSymbolSyncedProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IUserBalanceProvider _userBalanceProvider;
    
    
    public NFTInfoNewSyncedProvider(ILogger<SeedSymbolSyncedProvider> logger, IObjectMapper objectMapper, 
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewNewIndexRepository, 
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository, 
        IUserBalanceProvider userBalanceProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _nftInfoNewIndexRepository = nftInfoNewNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _userBalanceProvider = userBalanceProvider;
    }

    public async Task<IndexerNFTInfo> GetNFTInfoIndexAsync(string nftInfoId)
    {
        var isSeed = nftInfoId.Match(NFTSymbolBasicConstants.SeedIdPattern);
        var res = isSeed
            ? _objectMapper.Map<SeedSymbolIndex, IndexerNFTInfo>(await _seedSymbolIndexRepository.GetAsync(nftInfoId))
            : _objectMapper.Map<NFTInfoNewIndex, IndexerNFTInfo>(await _nftInfoNewIndexRepository.GetAsync(nftInfoId));
        if (res == null)
        {
            return null;
        }

        if (isSeed)
        {
            res.Generation = CommonConstant.IntNegativeOne;
            res.SeedOwnedSymbol = EnumDescriptionHelper.GetExtraInfoValue(res.ExternalInfoDictionary,
                TokenCreatedExternalInfoEnum.SeedOwnedSymbol, res.TokenName);
        }
        var balanceInfo = await _userBalanceProvider.GetNFTBalanceInfoAsync(nftInfoId);
        res.Owner = balanceInfo.Owner;
        res.OwnerCount = balanceInfo.OwnerCount;
        return res;
    }

    public async Task<Tuple<long, List<NFTInfoIndex>>> GetNFTBriefInfosAsync(GetCompositeNFTInfosInput dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
        var shouldQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();

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

        if (!dto.Generation.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Generation).Terms(dto.Generation)));
        }
        
        if (!dto.Traits.IsNullOrEmpty())
        {
            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            foreach (var trait in dto.Traits)
            {
                var key = trait.Key;
                var values = trait.Values;
        
                nestedQuery.Add(q => q
                    .Nested(n => n
                        .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                        .Query(nq => nq
                            .Bool(nb => nb
                                .Must(nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Key)
                                        .Query(key)
                                    )
                                )
                                .Filter(f => f
                                    .Terms(t => t
                                        .Field(ff => ff.TraitPairsDictionary.First().Value)
                                        .Terms(values)
                                    )
                                )
                            )
                        )
                    )
                );
            }
        
            mustQuery.AddRange(nestedQuery);
            
        }
       
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.CountedFlag).Value(true)));

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

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var sort = GetSortForNFTBrife(dto.Sorting);
        var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, sortFunc: sort, skip: dto.SkipCount, limit: dto.MaxResultCount);
        
        var nftInfoIndexList = result?.Item2.Select(item =>
        {
            var newItem = _objectMapper.Map<NFTInfoNewIndex, NFTInfoIndex>(item);
            newItem.PreviewImage = FTHelper.BuildIpfsUrl(newItem.PreviewImage);
            return newItem;
        }).ToList();
        
        if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
        {
            return new Tuple<long, List<NFTInfoIndex>>(result.Item1, nftInfoIndexList);
        }

        var count = await QueryRealCountAsync(mustQuery,null);
        var newResult = new Tuple<long, List<NFTInfoIndex>>(count, nftInfoIndexList);
        return newResult;
    }

    public async Task<IndexerNFTInfos> GetNFTInfosUserProfileAsync(GetNFTInfosProfileInput dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
        var sorting = new Tuple<SortOrder, Expression<Func<NFTInfoNewIndex, object>>>(SortOrder.Descending, o => o.LatestListingTime);
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
                    script.Source($"{NFTSymbolBasicConstants.BurnedAllNftScript}  || {NFTSymbolBasicConstants.IssuedLessThenOneGetThenZeroANftScript}")
                )
            )
        );
        
        QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        }
        var result = await _nftInfoNewIndexRepository.GetListAsync(Filter, sortType: sorting.Item1, sortExp: sorting.Item2,
            skip: skipCount, limit: dto.MaxResultCount);
         var indexerInfos = new IndexerNFTInfos
        {
            TotalRecordCount = result.Item1,
            IndexerNftInfos = _objectMapper.Map<List<NFTInfoNewIndex>, List<IndexerNFTInfo>>(result.Item2)
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

         var count = await QueryRealCountAsync(mustQuery, mustNotQuery);
         indexerInfos.TotalRecordCount = count;
         return indexerInfos;
    }

    private static void AddQueryForMinListingPrice(
        List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>> mustQuery,
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
    
    private static Func<SortDescriptor<NFTInfoNewIndex>, IPromise<IList<ISort>>> GetSortForNFTBrife(string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            throw new NotSupportedException();
        }

        SortDescriptor<NFTInfoNewIndex> sortDescriptor = new SortDescriptor<NFTInfoNewIndex>();
        
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
    
    private async Task<long> QueryRealCountAsync(List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>> mustQuery,
    List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>> mustNotQuery)
    {
        var countRequest = new SearchRequest<NFTInfoNewIndex>
        {
            Query = new BoolQuery
            {
                Must = mustQuery
                    .Select(func => func(new QueryContainerDescriptor<NFTInfoNewIndex>()))
                    .ToList()
                    .AsEnumerable(),
                MustNot = mustNotQuery.Select(func => func(new QueryContainerDescriptor<NFTInfoNewIndex>()))
                    .ToList()
                    .AsEnumerable()
            },
            Size = 0
        };
        
        Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer> queryFunc = q => countRequest.Query;
        var realCount = await _nftInfoNewIndexRepository.CountAsync(queryFunc);
        return realCount.Count;
    }
    
    
}