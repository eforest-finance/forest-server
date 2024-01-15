using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Nest;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;

namespace NFTMarketServer.Seed.Provider;

public interface ITsmSeedProvider
{
    Task<Tuple<long, List<TsmSeedSymbolIndex>>> GetBiddingSeedsAsync(GetBiddingSeedsInput input, 
        Expression<Func<TsmSeedSymbolIndex, object>> sortExp,
        SortOrder sortType);
}