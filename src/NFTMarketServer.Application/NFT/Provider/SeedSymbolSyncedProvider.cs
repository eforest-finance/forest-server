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
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT.Provider;

public interface ISeedSymbolSyncedProvider
{
    public Task<Tuple<long, List<SeedSymbolIndex>>> GetSeedBriefInfosAsync(GetCompositeNFTInfosInput dto);

    public Task<IndexerSeedInfos> GetSeedInfosUserProfileAsync(GetNFTInfosProfileInput dto);
}

public class SeedSymbolSyncedProvider : ISeedSymbolSyncedProvider, ISingletonDependency
{
    private readonly ILogger<SeedSymbolSyncedProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IUserBalanceProvider _userBalanceProvider;

    public SeedSymbolSyncedProvider(ILogger<SeedSymbolSyncedProvider> logger, 
        IObjectMapper objectMapper, 
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository, 
        IUserBalanceProvider userBalanceProvider)
    {
        _logger = logger;
        _objectMapper = objectMapper;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _userBalanceProvider = userBalanceProvider;
    }

    public async Task<Tuple<long, List<SeedSymbolIndex>>> GetSeedBriefInfosAsync(GetCompositeNFTInfosInput dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var shouldQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var shouldQuery2 = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        if (!dto.SearchParam.IsNullOrWhiteSpace())
        {
            shouldQuery2.Add(q => q.Term(i
                => i.Field(f => f.TokenName).Value(NFTSymbolBasicConstants.SeedNamePrefix +
                                                   NFTSymbolBasicConstants.NFTSymbolSeparator + dto.SearchParam)));
            shouldQuery2.Add(q
                => q.Term(i => i.Field(f => f.TokenName).Value(dto.SearchParam)));
        }

        if (!dto.ChainList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ChainId).Terms(dto.ChainList)));
        }

        if (!dto.SymbolTypeList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TokenType).Terms(dto.SymbolTypeList)));
        }

        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.Supply).GreaterThan(0)));
        mustQuery.Add(q => q.Bool(b => b.Must(m => m.Term(i => i.Field(f => f.IsDeleteFlag).Value(false)))));

        if (!dto.HasListingFlag && !dto.HasAuctionFlag && !dto.HasOfferFlag)
        {
            AddQueryForMinListingPriceAndMaxAuctionPrice(shouldQuery, dto);
        }

        if (dto.HasListingFlag)
        {
            AddPriceRangeQuery(mustQuery, dto, i => i.MinListingPrice);
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasListingFlag).Value(dto.HasListingFlag)))));
        }

        if (dto.HasAuctionFlag)
        {
            AddPriceRangeQuery(mustQuery, dto, i => i.MaxAuctionPrice);
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasAuctionFlag).Value(dto.HasAuctionFlag)))));
        }

        if (dto.HasOfferFlag)
        {
            mustQuery.Add(q => q.Bool(b => b.Must(m => m
                .Term(i => i.Field(f => f.HasOfferFlag).Value(dto.HasOfferFlag)))));
        }

        if (shouldQuery.Any())
        {
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }

        if (shouldQuery2.Any())
        {
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery2)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f) => f.Bool(b => b.Must(mustQuery));

        var sort = GetSortForSeedBrife(dto.Sorting);
        var result = await _seedSymbolIndexRepository.GetSortListAsync(Filter, sortFunc: sort, skip: dto.SkipCount,
            limit: dto.MaxResultCount);
        return result;
    }

    public async Task<IndexerSeedInfos> GetSeedInfosUserProfileAsync(GetNFTInfosProfileInput dto)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var mustNotQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        var sorting = new Tuple<SortOrder, Expression<Func<SeedSymbolIndex, object>>>(SortOrder.Descending, o => o.LatestListingTime);
        //only CollectionSymbol is xxx-SEED-O
        if (!dto.NFTCollectionId.IsNullOrEmpty() && !dto.NFTCollectionId.Match(NFTSymbolBasicConstants.SeedZeroIdPattern))
        {
            return IndexerSeedInfos.Init();
        }
        
        if (dto.Status == NFTSymbolBasicConstants.NFTInfoQueryStatusSelf)
        {
            //query match seed
            var indexerUserMatchedNft = await _userBalanceProvider.GetUserMatchedNftIdsAsync(dto, true);
            if (indexerUserMatchedNft == null || indexerUserMatchedNft.NftIds.IsNullOrEmpty())
            {
                return IndexerSeedInfos.Init();
            }
            mustQuery.Add(q => q.Ids(i => i.Values(indexerUserMatchedNft.NftIds)));
        }
        else
        {
            if (!dto.IssueAddress.IsNullOrEmpty())
            {
                mustQuery.Add(q => q.Term(i
                    => i.Field(f => f.IssuerTo).Value(dto.IssueAddress)));
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
            mustQuery.Add(q => q.Terms(i 
                => i.Field(f => f.Id).Terms(dto.NFTInfoIds)));
        }

        //Exclude Burned All NFT ( supply = 0 and issued = totalSupply)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{NFTSymbolBasicConstants.BurnedAllNftScript}")
                )
            )
        );
        mustNotQuery.Add(q => 
            q.Term(i => i.Field(f => f.ChainId).Value(NFTSymbolBasicConstants.MainChain)));
        
        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
        {
            return f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        }
        var result = await _seedSymbolIndexRepository.GetListAsync(Filter, sortType: sorting.Item1, sortExp: sorting.Item2,
            skip: dto.SkipCount, limit: dto.MaxResultCount);
        var indexerInfos = new IndexerSeedInfos
        {
            TotalRecordCount = result.Item1,
            IndexerSeedInfoList = _objectMapper.Map<List<SeedSymbolIndex>, List<IndexerSeedInfo>>(result.Item2)
        };
        return indexerInfos;
    }

    private static void AddQueryForMinListingPriceAndMaxAuctionPrice(
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> shouldQuery,
        GetCompositeNFTInfosInput dto)
    {
        //add MinListingPrice
        AddPriceQuery(shouldQuery, dto.PriceLow, dto.PriceHigh, f => f.MinListingPrice, f => f.HasListingFlag);
        //add MaxAuctionPrice
        AddPriceQuery(shouldQuery, dto.PriceLow, dto.PriceHigh, f => f.MaxAuctionPrice, f => f.HasAuctionFlag);
    }

    private static void AddPriceQuery(
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> shouldQuery,
        decimal? priceLow, decimal? priceHigh,
        Expression<Func<SeedSymbolIndex, object>> priceField,
        Expression<Func<SeedSymbolIndex, bool>> flagField)
    {
        if (priceLow != null && priceHigh != null)
        {
            shouldQuery.Add(q => q.Bool(m => m.Must(
                q => q.Term(i => i.Field(flagField).Value(true)),
                q => q.Range(i => i.Field(priceField).GreaterThanOrEquals(Convert.ToDouble(priceLow))),
                q => q.Range(i => i.Field(priceField).LessThanOrEquals(Convert.ToDouble(priceHigh))))));
        }
        else
        {
            if (priceLow != null)
            {
                shouldQuery.Add(q => q.Bool(m => m.Must(
                    q => q.Term(i => i.Field(flagField).Value(true)),
                    q => q.Range(i => i.Field(priceField).GreaterThanOrEquals(Convert.ToDouble(priceLow))))));
            }

            if (priceHigh != null)
            {
                shouldQuery.Add(q => q.Bool(m => m.Must(
                    q => q.Term(i => i.Field(flagField).Value(true)),
                    q => q.Range(i => i.Field(priceField).LessThanOrEquals(Convert.ToDouble(priceHigh))))));
            }
        }
    }


    private static void AddPriceRangeQuery(
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> mustQuery,
        GetCompositeNFTInfosInput dto,
        Expression<Func<SeedSymbolIndex, object>> priceField)
    {
        if (dto.PriceLow != null)
        {
            mustQuery.Add(q =>
                q.Range(i => i.Field(priceField).GreaterThanOrEquals(Convert.ToDouble(dto.PriceLow))));
        }

        if (dto.PriceHigh != null)
        {
            double minPrice = dto.PriceLow != null ? Convert.ToDouble(dto.PriceLow) : 0;
            mustQuery.Add(q => q.Range(i => i.Field(priceField).GreaterThanOrEquals(minPrice)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(priceField).LessThanOrEquals(Convert.ToDouble(dto.PriceHigh))));
        }
    }

    private static Func<SortDescriptor<SeedSymbolIndex>, IPromise<IList<ISort>>> GetSortForSeedBrife(string sorting)
    {
        if (string.IsNullOrWhiteSpace(sorting))
        {
            throw new NotSupportedException();
        }

        var sortDescriptor = new SortDescriptor<SeedSymbolIndex>();

        var sortingArray = sorting.Split(" ");
        var sortOrder = sortingArray.Length > 1 ? sortingArray[1] : "asc";

        switch (sortingArray[0])
        {
            case "Low":
                ApplyPriceSort(sortDescriptor, a => a.MinListingPrice, sortOrder);
                break;
            case "High":
                ApplyPriceSort(sortDescriptor, a => a.MaxAuctionPrice, sortOrder);
                break;
            case "Recently":
                sortDescriptor.Descending(a => a.CreateTime);
                break;
            default:
                sortDescriptor.Descending(a => a.LatestListingTime)
                    .Descending(a => a.AuctionDateTime)
                    .Descending(a => a.BlockHeight);
                break;
        }

        return s => sortDescriptor;
    }

    private static void ApplyPriceSort(SortDescriptor<SeedSymbolIndex> sortDescriptor,
        Expression<Func<SeedSymbolIndex, object>> field,
        string sortOrder)
    {
        sortDescriptor.Descending(a => a.HasListingFlag)
            .Descending(a => a.HasAuctionFlag)
            .Ascending(a => a.MaxOfferPrice)
            .Descending(a => a.CreateTime);

        if (sortOrder == "asc")
        {
            sortDescriptor.Ascending(field);
        }
        else
        {
            sortDescriptor.Descending(field);
        }
    }
}