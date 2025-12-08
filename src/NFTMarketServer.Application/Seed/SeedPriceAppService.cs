using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Seed;

public class SeedPriceAppService: NFTMarketServerAppService,ISeedPriceAppService
{
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<SeedPriceAppService> _logger;

    private readonly INESTRepository<SeedPriceIndex, string> _seedPriceIndexRepository;
    private readonly INESTRepository<UniqueSeedPriceIndex, string> _uniqueSeedPriceIndexRepository;
    private readonly INESTRepository<TsmSeedSymbolIndex, string> _tsmSeedIndexRepository;
    private const int QueryCount = 100;
    private const int MaxCount = 100;
    private const int MinNftSymbolLength = 4;
    public SeedPriceAppService(INESTRepository<SeedPriceIndex, string> seedPriceIndexRepository, INESTRepository<UniqueSeedPriceIndex, string> uniqueSeedPriceIndexRepository, ILogger<SeedPriceAppService> logger, IObjectMapper objectMapper, INESTRepository<TsmSeedSymbolIndex, string> tsmSeedIndexRepository)
    {
        _seedPriceIndexRepository = seedPriceIndexRepository;
        _uniqueSeedPriceIndexRepository = uniqueSeedPriceIndexRepository;
        _logger = logger;
        _objectMapper = objectMapper;
        _tsmSeedIndexRepository = tsmSeedIndexRepository;
    }

    public async Task<UniqueSeedPriceDto> GetUniqueSeedPriceInfoAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<UniqueSeedPriceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(id)));
        
        QueryContainer Filter(QueryContainerDescriptor<UniqueSeedPriceIndex> f) => f.Bool(b => b.Must(mustQuery));

        var uniqueSeedPriceIndex = await _uniqueSeedPriceIndexRepository.GetAsync(Filter);

        return uniqueSeedPriceIndex != null ? _objectMapper.Map<UniqueSeedPriceIndex, UniqueSeedPriceDto>(uniqueSeedPriceIndex) : null;
    }

    public async Task AddOrUpdateUniqueSeedPriceInfoAsync(UniqueSeedPriceDto uniqueSeedPriceDto)
    {
        var uniqueSeedPriceIndex = _objectMapper.Map<UniqueSeedPriceDto, UniqueSeedPriceIndex>(uniqueSeedPriceDto);
        await _uniqueSeedPriceIndexRepository.AddOrUpdateAsync(uniqueSeedPriceIndex);
    }

    public async Task<SeedPriceDto> GetSeedPriceInfoAsync(string id)
    {
        if (string.IsNullOrEmpty(id))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SeedPriceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(id)));
        
        QueryContainer Filter(QueryContainerDescriptor<SeedPriceIndex> f) => f.Bool(b => b.Must(mustQuery));

        var seedPriceIndex = await _seedPriceIndexRepository.GetAsync(Filter);

        return seedPriceIndex != null ? _objectMapper.Map<SeedPriceIndex, SeedPriceDto>(seedPriceIndex) : null;
    }

    public async Task AddOrUpdateSeedPriceInfoAsync(SeedPriceDto seedPriceDto)
    {
        var seedPriceIndex = _objectMapper.Map<SeedPriceDto, SeedPriceIndex>(seedPriceDto);
        await _seedPriceIndexRepository.AddOrUpdateAsync(seedPriceIndex);
    }

    public async Task<List<SeedDto>> GetUniqueSeedDtoNoBurnListAsync(GetTsmUniqueSeedInfoRequestDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeedType).Value(input.SeedType)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(input.isBurn)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Status).Value(input.Status)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) => f.Bool(b => b.Must(mustQuery));
        var sorting = new Func<SortDescriptor<TsmSeedSymbolIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.RegisterTime));

        var tuple = await _tsmSeedIndexRepository.GetSortListAsync(Filter,
            sortFunc: sorting,
            limit: input.MaxResultCount == 0 ? BidConsts.DefaultListSize : input.MaxResultCount > BidConsts.MaxListSize ? BidConsts.MaxListSize : input.MaxResultCount,
            skip: input.SkipCount);

        return tuple != null && !tuple.Item2.IsNullOrEmpty() ? _objectMapper.Map<List<TsmSeedSymbolIndex>, List<SeedDto>>(tuple.Item2) : new List<SeedDto>();
    }
    
    
    public async Task<List<SeedDto>> GetDisableSeedDtoListAsync(GetTsmUniqueSeedInfoRequestDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.SeedType).Value(input.SeedType)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) => f.Bool(b => b.Must(mustQuery));
        var sorting = new Func<SortDescriptor<TsmSeedSymbolIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.RegisterTime));

        var tuple = await _tsmSeedIndexRepository.GetSortListAsync(Filter,
            sortFunc: sorting,
            limit: input.MaxResultCount == 0 ? BidConsts.DefaultListSize : input.MaxResultCount > BidConsts.MaxListSize ? BidConsts.MaxListSize : input.MaxResultCount,
            skip: input.SkipCount);

        return tuple != null && !tuple.Item2.IsNullOrEmpty() ? _objectMapper.Map<List<TsmSeedSymbolIndex>, List<SeedDto>>(tuple.Item2) : new List<SeedDto>();
    }

    public async Task UpdateSeedPriceAsync(SeedDto seedDto)
    {
        var tsmSeedSymbolIndex = _objectMapper.Map<SeedDto,TsmSeedSymbolIndex>(seedDto);
        await _tsmSeedIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);
    }

    public async Task UpdateUniqueAllNoBurnSeedPriceAsync()
    {
        var uniqueSeedDtoNoBurnList = new List<SeedDto>();
        var requestDto = new GetTsmUniqueSeedInfoRequestDto
        {
            SkipCount = 0,
            MaxResultCount = MaxCount,
            SeedType = SeedType.UNIQUE,
            isBurn = false,
            Status = SeedStatus.AVALIABLE
        };
        do
        {
            uniqueSeedDtoNoBurnList = await GetUniqueSeedDtoNoBurnListAsync(requestDto);
            _logger.LogDebug("UpdateUniqueSeedPrice DoWorkAsync uniqueSeedDtoNoBurnList uniqueSeedDtoNoBurnList size: {Count}", uniqueSeedDtoNoBurnList.Count);

            foreach (var uniqueSeedDto in uniqueSeedDtoNoBurnList)
            {
                var symbolLength = uniqueSeedDto.Symbol.Length;
                var seedPriceId = IdGenerateHelper.GetSeedPriceId(uniqueSeedDto.TokenType.ToString(), symbolLength);
                var seedPriceInfoAsync = await GetSeedPriceInfoAsync(seedPriceId);
                var uniqueSeedPriceInfoAsync = await GetUniqueSeedPriceInfoAsync(seedPriceId);
                
                if (seedPriceInfoAsync == null ) return;
                if (uniqueSeedDto.TokenPrice.Symbol.IsNullOrEmpty())
                {
                    uniqueSeedDto.TokenPrice.Symbol = "ELF";
                }


                uniqueSeedDto.TokenPrice.Amount = seedPriceInfoAsync.TokenPrice.Amount;
                if (uniqueSeedPriceInfoAsync != null)
                {
                    if (seedPriceInfoAsync.TokenType.Equals("NFT"))
                    {
                        if (seedPriceInfoAsync.SymbolLength >= MinNftSymbolLength)
                        {
                            uniqueSeedDto.TokenPrice.Amount += uniqueSeedPriceInfoAsync.TokenPrice.Amount;
                        }
                    }
                    else
                    {
                        uniqueSeedDto.TokenPrice.Amount += uniqueSeedPriceInfoAsync.TokenPrice.Amount;
                    }
                }
                UpdateSeedPriceAsync(uniqueSeedDto);
            }
            requestDto.SkipCount += QueryCount;
        } while (!uniqueSeedDtoNoBurnList.IsNullOrEmpty());
    }
    
    
    public async Task UpdateDisableSeedPriceAsync()
    {
        var disableList = new List<SeedDto>();
        var requestDto = new GetTsmUniqueSeedInfoRequestDto
        {
            SkipCount = 0,
            MaxResultCount = MaxCount,
            SeedType = SeedType.DISABLE,
        };
        do
        {
            disableList = await GetDisableSeedDtoListAsync(requestDto);
            _logger.LogDebug("UpdateDisableSeedPriceAsync DoWorkAsync uniqueSeedDtoNoBurnList uniqueSeedDtoNoBurnList size: {Count}", disableList.Count);

            foreach (var uniqueSeedDto in disableList)
            {
                if (uniqueSeedDto.TokenPrice != null)
                {
                    uniqueSeedDto.TokenPrice.Amount = 0;
                }
                UpdateSeedPriceAsync(uniqueSeedDto);
            }
            requestDto.SkipCount += QueryCount;
        } while (!disableList.IsNullOrEmpty());
    }
}