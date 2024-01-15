using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Forest.Indexer.Plugin.enums;
using Google.Protobuf.WellKnownTypes;
using Nest;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Seed.Provider;

public class TsmSeedProvider : ITsmSeedProvider, ISingletonDependency
{
    private readonly INESTRepository<TsmSeedSymbolIndex, string> _tsmSeedSymbolIndexRepository;

    public TsmSeedProvider(INESTRepository<TsmSeedSymbolIndex, string> tsmSeedSymbolIndexRepository)
    {
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
    }


    public async Task<Tuple<long, List<TsmSeedSymbolIndex>>> GetBiddingSeedsAsync(GetBiddingSeedsInput input,
        Expression<Func<TsmSeedSymbolIndex, object>> sortExp,
        SortOrder sortType)
    {
        var mustQuery = BuildMustQuery(input);

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        
        var indexerSpecialSeeds = await _tsmSeedSymbolIndexRepository.GetListAsync(Filter, sortExp: sortExp,
            sortType: sortType, skip: input.SkipCount, limit: input.MaxResultCount);

        return indexerSpecialSeeds;
    }

    private static List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>> BuildMustQuery(
        GetBiddingSeedsInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        if (!input.ChainIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i
                => i.Field(f => f.ChainId).Terms(input.ChainIds)));
        }

        if (!input.TokenTypes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i
                => i.Field(f => f.TokenType).Terms(input.TokenTypes)));
        }

        if (!input.SeedTypes.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i
                => i.Field(f => f.SeedType).Terms(input.SeedTypes)));
        }

        if (input.SymbolLengthMin != null)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.SymbolLength).GreaterThanOrEquals(input.SymbolLengthMin)));
        }

        if (input.SymbolLengthMax != null)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.SymbolLength).LessThanOrEquals(input.SymbolLengthMax)));
        }

        if (input.PriceMin != null)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.TopBidPrice.Amount).GreaterThanOrEquals(input.PriceMin)));
        }

        if (input.PriceMax != null)
        {
            mustQuery.Add(q => q.Range(i
                => i.Field(f => f.TopBidPrice.Amount).LessThanOrEquals(input.PriceMax)));
        }

        //add AuctionEndTime > current 
        mustQuery.Add(q => q.Range(i 
            => i.Field(f => f.AuctionEndTime).GreaterThan(DateTime.Now.ToUniversalTime().ToTimestamp().Seconds)));
        //only query bidding
        mustQuery.Add(q => q.Term(i
            => i.Field(f => f.AuctionStatus).Value(SeedAuctionStatus.Bidding)));
        mustQuery.Add(q => q.Term(i
            => i.Field(f => f.IsBurned).Value(false)));
        mustQuery.Add(q => q.Terms(i

            => i.Field(f => f.Status).Terms(SeedStatus.AVALIABLE, SeedStatus.UNREGISTERED)));

        return mustQuery;
    }
}