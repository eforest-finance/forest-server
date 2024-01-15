using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Common;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.Synchronize;
using NFTMarketServer.NFT;
using NFTMarketServer.Options;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Seed.Provider;
using NFTMarketServer.Synchronize.Eto;
using NFTMarketServer.Synchronize.Provider;
using NFTMarketServer.Tokens;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Seed;

public class SeedAppService : NFTMarketServerAppService, ISeedAppService
{
    private static readonly List<string> AllType = new() { TokenType.FT.ToString(), TokenType.NFT.ToString() };

    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<SeedAppService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly ISynchronizeTransactionProvider _synchronizeTransactionProvider;
    private readonly ITokenAppService _tokenAppService;
    private readonly IBidAppService _bidAppService;
    private readonly ISeedProvider _seedProvider;
    private readonly ISymbolIconAppService _symbolIconAppService;
    private readonly IOptionsMonitor<TransactionFeeOption> _optionsMonitor;
    private readonly INESTRepository<TsmSeedSymbolIndex, string> _tsmSeedSymbolIndexRepository;
    private readonly INESTRepository<SeedPriceIndex, string> _seedPriceIndexRepository;
    private readonly INESTRepository<UniqueSeedPriceIndex, string> _uniqueSeedPriceIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;

    private readonly ITsmSeedProvider _tsmSeedProvider;
    private const decimal MinMarkupDenominator = 10000;

    public SeedAppService(ILogger<SeedAppService> logger, ITokenAppService tokenAppService,
        IBidAppService bidAppService, ISeedProvider seedProvider,
        ISymbolIconAppService symbolIconAppService, IObjectMapper objectMapper, IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, ISynchronizeTransactionProvider synchronizeTransactionProvider,
        INESTRepository<TsmSeedSymbolIndex, string> tsmSeedSymbolIndexRepository,
        INESTRepository<SeedPriceIndex, string> seedPriceIndexRepository,
        INESTRepository<UniqueSeedPriceIndex, string> uniqueSeedPriceIndexRepository,
        IOptionsMonitor<TransactionFeeOption> optionsMonitor,
        ITsmSeedProvider tsmSeedProvider, 
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository)
    {
        _objectMapper = objectMapper;
        _logger = logger;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _synchronizeTransactionProvider = synchronizeTransactionProvider;
        _tokenAppService = tokenAppService;
        _bidAppService = bidAppService;
        _seedProvider = seedProvider;
        _symbolIconAppService = symbolIconAppService;
        _tsmSeedSymbolIndexRepository = tsmSeedSymbolIndexRepository;
        _seedPriceIndexRepository = seedPriceIndexRepository;
        _uniqueSeedPriceIndexRepository = uniqueSeedPriceIndexRepository;
        _optionsMonitor = optionsMonitor;
        _tsmSeedProvider = tsmSeedProvider;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
    }
    
    public async Task<CreateSeedResultDto> CreateSeedAsync(CreateSeedDto input)
    {
        try
        {
            var syncData = await _synchronizeTransactionProvider.GetSynchronizeJobBySymbolAsync(input.Seed);
            if (!string.IsNullOrEmpty(syncData.TxHash) || !string.IsNullOrEmpty(syncData.Status))
            {
                _logger.LogWarning("This seed {Symbol} status {Status} had registry.", input.Seed, syncData.Status);
                throw new UserFriendlyException($"This seed {input.Seed} status {syncData.Status} had registry.");
            }

            var id = GuidGenerator.Create().ToString();
            var synchronizeTransactionJobGrain = _clusterClient.GetGrain<ISynchronizeTxJobGrain>(id);

            var createSeedJob = _objectMapper.Map<CreateSeedDto, CreateSeedJobGrainDto>(input);
            createSeedJob.Status = SynchronizeTransactionJobStatus.SeedCreating;

            _logger.LogInformation("id {ID} Seed: {seed} Synchronize-Transaction-Job will be created", id, input.Seed);
            var result = await synchronizeTransactionJobGrain.CreateSeedJobAsync(createSeedJob);

            if (!result.Success)
            {
                _logger.LogError("Create seed Job fail, seed: {seed}.", input.Seed);
                throw new UserFriendlyException($"Create seed Job fail, seed: {input.Seed}.");
            }

            var etoData = _objectMapper.Map<SynchronizeTxJobGrainDto, SynchronizeTransactionInfoEto>(result.Data);
            etoData.Id = id;
            etoData.LastModifyTime = TimeStampHelper.GetTimeStampInMilliseconds();
            _logger.LogInformation("id {ID} TxHash: {TxHash} Synchronize-Job will be created", id, etoData.TxHash);
            await _distributedEventBus.PublishAsync(etoData);
            return new CreateSeedResultDto()
            {
                TxHash = etoData.TxHash
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "An error occurred during synchronous job. Symbol:{Symbol}", input.Seed);
            throw new UserFriendlyException($"An error occurred during synchronous job. Symbol:{input.Seed}");
        }
    }

    public async Task<PagedResultDto<SpecialSeedDto>> GetSpecialSymbolListAsync(QuerySpecialListInput input)
    {
        if (input == null)
        {
            return new PagedResultDto<SpecialSeedDto>
            {
                Items = new List<SpecialSeedDto>(),
                TotalCount = 0
            };
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        if (input.ChainIds != null && input.ChainIds.Any())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.ChainId).Terms(input.ChainIds)));
        }

        if (input.TokenTypes != null && input.TokenTypes.Any())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TokenType).Terms(input.TokenTypes)));
        }

        if (input.SeedTypes != null && input.SeedTypes.Any())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.SeedType).Terms(input.SeedTypes)));
        }

        if (input.SymbolLengthMin != null)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.SymbolLength).GreaterThanOrEquals(input.SymbolLengthMin)));
        }

        if (input.SymbolLengthMax != null)
        {
            mustQuery.Add(q => q.Range(i => i.Field(f => f.SymbolLength).LessThanOrEquals(input.SymbolLengthMax)));
        }

        if (input.PriceMin != null)
        {
            var shouldPriceMinQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            
            var shouldPriceMinMustTokenPriceQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMinMustTokenPriceQuery.Add(q => q.Range(i => i.Field(f => f.TokenPrice.Amount).GreaterThan(0)));
            shouldPriceMinMustTokenPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TokenPrice.Amount).GreaterThanOrEquals(input.PriceMin)));
            var shouldPriceMinMustTopBidPriceQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMinMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).GreaterThan(0)));
            shouldPriceMinMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).GreaterThanOrEquals(input.PriceMin)));

            shouldPriceMinQuery.Add(q => q.Bool(b => b.Must(shouldPriceMinMustTokenPriceQuery)));
            shouldPriceMinQuery.Add(q => q.Bool(b => b.Must(shouldPriceMinMustTopBidPriceQuery)));
            
            mustQuery.Add(q=>q.Bool(b=>b.Should(shouldPriceMinQuery)));
        }

        if (input.PriceMax != null)
        {
            var shouldPriceMaxQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            
            var shouldPriceMaxMustTokenPriceQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMaxMustTokenPriceQuery.Add(q => q.Range(i => i.Field(f => f.TokenPrice.Amount).GreaterThan(0)));
            shouldPriceMaxMustTokenPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TokenPrice.Amount).LessThanOrEquals(input.PriceMax)));
            var shouldPriceMaxMustTopBidPriceQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMaxMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).GreaterThan(0)));
            shouldPriceMaxMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).LessThanOrEquals(input.PriceMax)));
            
            shouldPriceMaxQuery.Add(q => q.Bool(b => b.Must(shouldPriceMaxMustTokenPriceQuery)));
            shouldPriceMaxQuery.Add(q => q.Bool(b => b.Must(shouldPriceMaxMustTopBidPriceQuery)));
            
            mustQuery.Add(q=>q.Bool(b=>b.Should(shouldPriceMaxQuery)));
        }
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(SeedStatus.AVALIABLE, SeedStatus.UNREGISTERED)));
        
        var shouldQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        shouldQuery.Add(q => q.Term(i => i.Field(f => f.AuctionEndTime).Value(0)));
        shouldQuery.Add(q => q.Range(i => i.Field(f => f.AuctionEndTime).GreaterThan(DateTime.Now.ToUniversalTime().ToTimestamp().Seconds)));
        mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));

        var mustNotQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustNotQuery.Add(q=>q.Term(i=>i.Field(f=>f.AuctionStatus).Value(AuctionFinishType.Finished)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        
        SortDescriptor<TsmSeedSymbolIndex> sortDescriptor = new SortDescriptor<TsmSeedSymbolIndex>();
        sortDescriptor.Descending(a=>a.RankingWeight);
        sortDescriptor.Ascending(a=>a.Symbol);
        IPromise<IList<ISort>> promise = sortDescriptor;

        var indexerSpecialSeeds = await _tsmSeedSymbolIndexRepository.GetSortListAsync(Filter,
            sortFunc: s => promise, skip: input.SkipCount, limit: input.MaxResultCount);
        var list = ObjectMapper.Map<List<TsmSeedSymbolIndex>, List<SpecialSeedItem>>(indexerSpecialSeeds.Item2);
        if (list.IsNullOrEmpty())
        {
            return new PagedResultDto<SpecialSeedDto>
            {
                Items = new List<SpecialSeedDto>(),
                TotalCount = 0
            };
        }

        var result = new List<SpecialSeedDto>();
        foreach (var info in list)
        {
            var seedDto = _objectMapper.Map<SpecialSeedItem, SpecialSeedDto>(info);
            if (info.SeedSymbol.IsNullOrEmpty())
            {
                result.Add(seedDto);
                continue;
            }

            var specialSeedAuctionInfo = await _bidAppService.GetSeedAuctionInfoAsync(info.SeedSymbol);
            if (specialSeedAuctionInfo == null)
            {
                result.Add(seedDto);
                continue;
            }

            seedDto.AuctionEndTime = specialSeedAuctionInfo.AuctionEndTime;
            seedDto.TopBidPrice = specialSeedAuctionInfo.TopBidPrice;
            if (!specialSeedAuctionInfo.BidderList.IsNullOrEmpty())
            {
                seedDto.BidsCount = specialSeedAuctionInfo.BidderList.Count;
                seedDto.BiddersCount = specialSeedAuctionInfo.BidderList.Distinct().ToList().Count;
            }

            result.Add(seedDto);
        }

        var countResponse= await _tsmSeedSymbolIndexRepository.CountAsync(Filter);
        var totalCount = 0L;
        if (countResponse != null)
        {
            totalCount= countResponse.Count;
        }
        
        return new PagedResultDto<SpecialSeedDto>
        {
            Items = result,
            TotalCount = totalCount
        };
    }
    
    public async Task<PagedResultDto<BiddingSeedDto>> GetBiddingSeedsAsync(GetBiddingSeedsInput input)
    {
        //1.order by  TopBidPrice asc
        var tuple = await _tsmSeedProvider.GetBiddingSeedsAsync(input,
                s => s.TopBidPrice.Amount,
            SortOrder.Ascending);
        var dataList = tuple.Item2;
        if (dataList.IsNullOrEmpty())
        {
            return new PagedResultDto<BiddingSeedDto>
            {
                Items = new List<BiddingSeedDto>(),
                TotalCount = 0
            };
        }
        //2.order by  TopBidPrice asc，BiddersCount desc，AuctionEndTime asc
        var resultList = dataList
            .OrderBy(s => s.TopBidPrice.Amount)
            .ThenByDescending(s => s.BiddersCount)
            .ThenBy(s => s.AuctionEndTime)
            .Select(s => ObjectMapper.Map<TsmSeedSymbolIndex, BiddingSeedDto>(s))
            .ToList();
        return new PagedResultDto<BiddingSeedDto>
        {
            Items = resultList,
            TotalCount = tuple.Item1
        };
    }

    public async Task<BidPricePayInfoDto> GetSymbolBidPriceAsync(QueryBidPricePayInfoInput input)
    {
        var bidInfoListRequest = new GetSymbolBidInfoListRequestDto()
        {
            SeedSymbol = input.Symbol
        };
        
        decimal elfBidPrice = 0;
        var auctionInfoList = await _bidAppService.GetSymbolAuctionInfoListAsync(input.Symbol);
        decimal minMarkup = 0;
        if (!auctionInfoList.IsNullOrEmpty())
        {
            minMarkup = auctionInfoList[0].MinMarkup;
            if (auctionInfoList[0].StartPrice != null && auctionInfoList[0].StartPrice.Amount > 0)
            {
                elfBidPrice = FTHelper.GetRealELFAmount(auctionInfoList[0].StartPrice.Amount);
            }
            if (auctionInfoList[0].FinishPrice != null && auctionInfoList[0].FinishPrice.Amount > 0)
            {
                elfBidPrice = FTHelper.GetRealELFAmount(auctionInfoList[0].FinishPrice.Amount);
            }
        }
        var dollarPrice = await _tokenAppService.GetCurrentDollarPriceAsync(SymbolHelper.CoinGeckoELF(), elfBidPrice);
        var minElfPriceMarkup = elfBidPrice * (minMarkup / MinMarkupDenominator);

        var minDollarPriceMarkup = await _tokenAppService.GetCurrentDollarPriceAsync(SymbolHelper.CoinGeckoELF(), minElfPriceMarkup);

        var tokenMarketData = await _tokenAppService.GetTokenMarketDataAsync(SymbolHelper.CoinGeckoELF(), null);
        var bidPricePayInfo = new BidPricePayInfoDto
        {
            DollarExchangeRate = tokenMarketData.Price,
            ElfBidPrice = elfBidPrice,
            DollarBidPrice = dollarPrice,
            MinMarkup = minMarkup / MinMarkupDenominator,
            MinDollarPriceMarkup = minDollarPriceMarkup,
            MinElfPriceMarkup = minElfPriceMarkup
        };
        return bidPricePayInfo;
    }

    public async Task<SeedDto> SearchSeedInfoAsync(SearchSeedInput input)
    {
        try
        {
            input.Symbol = input.Symbol.ToUpper();
            AssertHelper.IsTrue(AllType.Contains(input.TokenType), "Invalid symbol type");
            AssertHelper.IsTrue(input.TokenType == TokenType.FT.ToString()
                ? SymbolHelper.MatchSymbolPattern(input.Symbol)
                : SymbolHelper.MatchNFTPrefix(input.Symbol), "Invalid symbol pattern");

            //Get seed info from indexer
            var seedInfoDto = await _seedProvider.SearchSeedInfoAsync(input);
            
            //Get token price from tsm seed symbol index
            var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(seedInfoDto.Symbol)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));
            QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
                => f.Bool(b => b.Must(mustQuery));
            var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetAsync(Filter);
            if (tsmSeedSymbolIndex != null && tsmSeedSymbolIndex.TokenPrice != null)
            {
                seedInfoDto.TokenPrice = new PriceInfo()
                {
                    Amount = Convert.ToInt64(tsmSeedSymbolIndex.TokenPrice.Amount),
                    Symbol = tsmSeedSymbolIndex.TokenPrice.Symbol
                };
            }
            
            //recalculate token price for unique seed
            if (seedInfoDto.Status == SeedStatus.AVALIABLE && 
                (seedInfoDto.SeedType == SeedType.Regular|| seedInfoDto.SeedType == SeedType.UNIQUE))
            {
                var seedPriceId = IdGenerateHelper.GetSeedPriceId(input.TokenType, seedInfoDto.Symbol.Length);
                var seedPriceIndex = await _seedPriceIndexRepository.GetAsync(seedPriceId);
                var uniqueSeedPriceIndex = await _uniqueSeedPriceIndexRepository.GetAsync(seedPriceId);
                if (seedPriceIndex != null)
                {
                    seedInfoDto.TokenPrice = new PriceInfo()
                    {
                        Amount = Convert.ToInt64(seedPriceIndex.TokenPrice.Amount),
                        Symbol = seedPriceIndex.TokenPrice.Symbol
                    };
                    if (uniqueSeedPriceIndex != null && seedInfoDto.SeedType == SeedType.UNIQUE)
                    {
                        seedInfoDto.TokenPrice.Amount += Convert.ToInt64(uniqueSeedPriceIndex.TokenPrice.Amount);
                    }
                }
            }
            
            if (!seedInfoDto.SeedSymbol.IsNullOrEmpty())
            {
                await _symbolIconAppService.GetIconBySymbolAsync(seedInfoDto.SeedSymbol, seedInfoDto.Symbol);
            }

            return await ConvertSeedDtoAsync(seedInfoDto);
        }
        catch (UserFriendlyException e)
        {
            throw;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Search Seed info error, symbol={Symbol}, type={Type}", input.Symbol, input.TokenType);
            throw new UserFriendlyException("Internal error, please try again later.");
        }
    }

    private async Task<SeedDto> ConvertSeedDtoAsync(SeedInfoDto seedInfoDto)
    {
        var seedDto = _objectMapper.Map<SeedInfoDto, SeedDto>(seedInfoDto);
        seedDto.ChainId = seedInfoDto.CurrentChainId;
        if (seedDto.TokenPrice != null)
        {
            seedDto.TokenPrice.Amount = FTHelper.GetRealELFAmount(seedDto.TokenPrice.Amount);
            var usdPrice =
                await _tokenAppService.GetCurrentDollarPriceAsync(seedDto.TokenPrice.Symbol, seedDto.TokenPrice.Amount);
            seedDto.UsdPrice = new TokenPriceDto()
            {
                Amount = usdPrice,
                Symbol = SeedConstants.UsdSymbol
            };
        }

        if (seedDto.TopBidPrice != null)
        {
            seedDto.TopBidPrice.Amount = FTHelper.GetRealELFAmount(seedDto.TopBidPrice.Amount);

            if (seedDto.TopBidPrice.Amount > 0 && seedDto.SeedType == SeedType.UNIQUE)
            {
                var usdPrice =
                    await _tokenAppService.GetCurrentDollarPriceAsync(seedDto.TopBidPrice.Symbol, seedDto.TopBidPrice.Amount);
                seedDto.UsdPrice = new TokenPriceDto()
                {
                    Amount = usdPrice,
                    Symbol = SeedConstants.UsdSymbol
                };
            }
        }

        if (!seedInfoDto.SeedSymbol.IsNullOrEmpty())
        {
            await _symbolIconAppService.GetIconBySymbolAsync(seedInfoDto.SeedSymbol, seedInfoDto.Symbol);
        }
        return seedDto;
    }

    public async Task<SeedDto> GetSeedInfoAsync(QuerySeedInput input)
    {
        //Get tsm seed info from indexer
        var seedInfoDto = await _seedProvider.GetSeedInfoAsync(input);
        
        //Get token price from tsm seed symbol index
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.Symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));
        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));
        var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetAsync(Filter);
        if (tsmSeedSymbolIndex != null && tsmSeedSymbolIndex.TokenPrice != null)
        {
            seedInfoDto.TokenPrice = new PriceInfo()
            {
                Amount = Convert.ToInt64(tsmSeedSymbolIndex.TokenPrice.Amount),
                Symbol = tsmSeedSymbolIndex.TokenPrice.Symbol
            };
        }

        return await ConvertSeedDtoAsync(seedInfoDto);
    }

    public async Task<PagedResultDto<SeedDto>> MySeedAsync(MySeedInput input)
    {
        AssertHelper.NotNull(input.Address, "Address empty");
        input.Address = input.Address.Where(addr => addr.NotNullOrEmpty()).ToList();
        AssertHelper.NotEmpty(input.Address, "Address invalid");
        var seedInfoDto = await _seedProvider.MySeedAsync(input);
        var mySeedDto = new PagedResultDto<SeedDto>()
        {
            TotalCount = seedInfoDto.TotalRecordCount,
            Items = seedInfoDto.SeedDtoList
        };
        return mySeedDto;
    }

    public async Task<TransactionFeeDto> GetTransactionFeeAsync()
    {
        var transactionFeeOption = _optionsMonitor.CurrentValue;
        var marketData = await _tokenAppService.GetTokenMarketDataAsync(SymbolHelper.CoinGeckoELF(), null);
        var result = decimal.Multiply(transactionFeeOption.TransactionFee, marketData.Price);
        var roundedResult = Math.Round(result, transactionFeeOption.Decimals);
        return new TransactionFeeDto
        {
            TransactionFee = transactionFeeOption.TransactionFee,
            TransactionFeeOfUsd = roundedResult
        };
    }

    public async Task AddOrUpdateTsmSeedInfoAsync(SeedDto seedDto)
    {
        var tsmSeedSymbolIndex = ObjectMapper.Map<SeedDto, TsmSeedSymbolIndex>(seedDto);
        
        //in case the price information is overwritten by indexer seed info
        _logger.LogInformation("AddOrUpdateTsmSeedInfoAsync {0}", tsmSeedSymbolIndex.Id);
        var currentTsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetAsync(tsmSeedSymbolIndex.Id);

        if (currentTsmSeedSymbolIndex != null)
        {
            if (tsmSeedSymbolIndex.TokenPrice == null ||
                (tsmSeedSymbolIndex.TokenPrice != null && tsmSeedSymbolIndex.TokenPrice.Amount <= 0))
            {
                _logger.LogInformation(
                    "AddOrUpdateTsmSeedInfoAsync currentTsmSeedSymbolIndex Amount {0} tsmSeedSymbolIndex Amount {1} {2}",
                    tsmSeedSymbolIndex.TokenPrice == null ? "" : tsmSeedSymbolIndex.TokenPrice.Amount,
                    currentTsmSeedSymbolIndex.TokenPrice == null ? "" : currentTsmSeedSymbolIndex.TokenPrice.Amount,
                    currentTsmSeedSymbolIndex.Id);
                tsmSeedSymbolIndex.TokenPrice = currentTsmSeedSymbolIndex.TokenPrice;
            }
            tsmSeedSymbolIndex.RankingWeight = currentTsmSeedSymbolIndex.RankingWeight;
        }
        else
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(seedDto.Symbol)));
            QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
                => f.Bool(b => b.Must(mustQuery));
            var otherChainTsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetAsync(Filter);
            if (otherChainTsmSeedSymbolIndex != null)
            {
                if (tsmSeedSymbolIndex.TokenPrice == null ||
                    (tsmSeedSymbolIndex.TokenPrice != null && tsmSeedSymbolIndex.TokenPrice.Amount <= 0))
                {
                    _logger.LogInformation(
                        "AddOrUpdateTsmSeedInfoAsync otherChainTsmSeedSymbolIndex Amount {0} tsmSeedSymbolIndex Amount {1} {2}",
                        tsmSeedSymbolIndex.TokenPrice == null ? "" : tsmSeedSymbolIndex.TokenPrice.Amount,
                        otherChainTsmSeedSymbolIndex.TokenPrice == null ? "" : otherChainTsmSeedSymbolIndex.TokenPrice.Amount,
                        otherChainTsmSeedSymbolIndex.Id);
                    tsmSeedSymbolIndex.TokenPrice = otherChainTsmSeedSymbolIndex.TokenPrice;
                }
                tsmSeedSymbolIndex.RankingWeight = otherChainTsmSeedSymbolIndex.RankingWeight;
            }
        }
        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);
    }

    public async Task AddOrUpdateSeedSymbolAsync(SeedSymbolIndex seedSymbol)
    {
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbol);
    }
    public async Task UpdateSeedRankingWeightAsync(List<SeedRankingWeightDto> inputList)
    {
        if (inputList.IsNullOrEmpty())
        {
            return;
        }

        foreach (var rankingWeightDto in inputList)
        {
            //Get tsm seed symbol index
            var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(rankingWeightDto.Symbol)));
            QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
                => f.Bool(b => b.Must(mustQuery));
            var result = await _tsmSeedSymbolIndexRepository.GetListAsync(Filter);
            if (result == null || result.Item2.IsNullOrEmpty())
            {
                throw new UserFriendlyException($"Symbol {rankingWeightDto.Symbol} not found");
            }

            //Update ranking weight
            foreach (var tsmSeedSymbolIndex in result.Item2)
            {
                _logger.LogInformation("UpdateSeedRankingWeightAsync {0} CurrentWeight {1}", tsmSeedSymbolIndex.Id,
                    tsmSeedSymbolIndex.RankingWeight);
                tsmSeedSymbolIndex.RankingWeight = rankingWeightDto.RankingWeight;
                await _tsmSeedSymbolIndexRepository.UpdateAsync(tsmSeedSymbolIndex);
                _logger.LogInformation("UpdateSeedRankingWeightAsync {0} successfully, NewWeight {1}", tsmSeedSymbolIndex.Id,
                    tsmSeedSymbolIndex.RankingWeight);
            }
        } 

    }

    /// <summary>
    /// Get tsm seeds which RankingWeight greater than 0
    /// </summary>
    /// <returns></returns>
    public async Task<PagedResultDto<SeedRankingWeightDto>> GetSeedRankingWeightInfosAsync()
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i => i.Field(f => f.RankingWeight).GreaterThan(0)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _tsmSeedSymbolIndexRepository.GetListAsync(Filter);
        var seedRankingWeightDtoList =
            ObjectMapper.Map<List<TsmSeedSymbolIndex>, List<SeedRankingWeightDto>>(result.Item2);
        seedRankingWeightDtoList = seedRankingWeightDtoList.DistinctBy(s => s.Symbol).OrderByDescending(o => o.RankingWeight)
            .OrderByDescending(o => o.Symbol)
            .ToList();
        return new PagedResultDto<SeedRankingWeightDto>()
        {
            TotalCount = seedRankingWeightDtoList.Count,
            Items = seedRankingWeightDtoList
        };
    }

}