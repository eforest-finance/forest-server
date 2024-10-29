using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Seed.Index;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT;

[RemoteService(IsEnabled = false)]
public class SeedOwnedSymbolAppService : NFTMarketServerAppService, ISeedOwnedSymbolAppService
{
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IObjectMapper _objectMapper;

    public SeedOwnedSymbolAppService(
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository
        , IObjectMapper objectMapper)
    {
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _objectMapper = objectMapper;
    }

    public async Task<PagedResultDto<SeedSymbolIndexDto>> GetAllSeedOwnedSymbolsAsync(GetAllSeedOwnedSymbols input)
    {
        if (input.SkipCount < 0) return buildInitSeedSymbolIndexDto();
        var seedOwnedSymbol = input.SeedOwnedSymbol;

        var seedOwnedSymbolIndexs =
            await DoAllSeedSymbolsAsync(input.SkipCount,
                input.MaxResultCount, input.AddressList, seedOwnedSymbol);
        if (seedOwnedSymbolIndexs == null) return buildInitSeedSymbolIndexDto();

        var totalCount = seedOwnedSymbolIndexs.TotalRecordCount;
        if (seedOwnedSymbolIndexs.IndexerSeedOwnedSymbolList == null)
        {
            return buildInitSeedSymbolIndexDto();
        }

        List<SeedSymbolIndexDto> symbolIndexDtos = _objectMapper.Map<List<IndexerSeedOwnedSymbol>
            , List<SeedSymbolIndexDto>>(seedOwnedSymbolIndexs.IndexerSeedOwnedSymbolList);

        return new PagedResultDto<SeedSymbolIndexDto>
        {
            Items = symbolIndexDtos,
            TotalCount = totalCount
        };
    }
    
    private async Task<IndexerSeedOwnedSymbols> DoAllSeedSymbolsAsync(
        int skip,
        int maxResultCount, List<string> addressList, string seedOwnedSymbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        if (addressList.IsNullOrEmpty())
        {
            return new IndexerSeedOwnedSymbols
            {
                TotalRecordCount = 0,
                IndexerSeedOwnedSymbolList = new List<IndexerSeedOwnedSymbol>()
            };
        }

        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.IssuerTo).Terms(addressList)));

        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.IsDeleteFlag).Value(false)));
        
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.TokenType).Value(TokenType.NFT)));

        mustQuery.Add(q=>
                q.DateRange(i =>
                    i.Field(f => f.SeedExpTime)
                        .GreaterThan(DateTime.Now))
            );

        if (!seedOwnedSymbol.IsNullOrWhiteSpace())
        {
            mustQuery.Add(q => q.Wildcard(i =>
                i.Field(f => f.SeedOwnedSymbol).Value("*" + seedOwnedSymbol + "*")));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f) => f.Bool(b => b.Must(mustQuery));


        IPromise<IList<ISort>> Sort(SortDescriptor<SeedSymbolIndex> s) =>
            s.Ascending(a => a.ChainId)
                .Script(script => script.Type(CommonConstant.SortTypeNumber)
                    .Script(scriptDescriptor => scriptDescriptor.Source(CommonConstant.SortScriptSourceValueLength))
                    .Order(SortOrder.Ascending))
                .Ascending(a => a.SeedOwnedSymbol)
                .Ascending(a => a.Id);

        var result = await _seedSymbolIndexRepository.GetSortListAsync(Filter, sortFunc: Sort,
            skip: skip,limit: maxResultCount);
        var dataList = _objectMapper.Map<List<SeedSymbolIndex>, List<IndexerSeedOwnedSymbol>>(result.Item2);
        var pageResult = new IndexerSeedOwnedSymbols
        {
            TotalRecordCount = result.Item1,
            IndexerSeedOwnedSymbolList = dataList
        };
        return pageResult;
    }

    private PagedResultDto<SeedSymbolIndexDto> buildInitSeedSymbolIndexDto()
    {
        return new PagedResultDto<SeedSymbolIndexDto>
        {
            Items = new List<SeedSymbolIndexDto>(),
            TotalCount = 0
        };
    }
}