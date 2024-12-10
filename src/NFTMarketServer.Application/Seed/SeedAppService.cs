using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Indexing.Elasticsearch;
using Forest.Contracts.SymbolRegistrar;
using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Bid;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Common;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.Synchronize;
using NFTMarketServer.HandleException;
using NFTMarketServer.Helper;
using NFTMarketServer.Market;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Seed.Provider;
using NFTMarketServer.Synchronize.Eto;
using NFTMarketServer.Synchronize.Provider;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Orleans;
using Orleans.Runtime;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Caching;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Exception = System.Exception;
using SeedType = NFTMarketServer.Seed.Dto.SeedType;
using TokenType = NFTMarketServer.Seed.Dto.TokenType;

namespace NFTMarketServer.Seed;

public class SeedAppService : NFTMarketServerAppService, ISeedAppService
{
    private static readonly List<string> AllType = new() { TokenType.FT.ToString(), TokenType.NFT.ToString() };

    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<SeedAppService> _logger;
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedCache<PriceInfo> _distributedCache;
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
    private readonly IUserAppService _userAppService;
    private readonly IOptionsMonitor<SeedRenewOptions> _seedRenewOptionsMonitor;
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;

    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ITsmSeedProvider _tsmSeedProvider;
    private readonly INFTListingProvider _nftListingProvider;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly INFTDealInfoProvider _dealInfoProvider;
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private const decimal MinMarkupDenominator = 10000;
    private const double ExpirationDays = 2;

    public SeedAppService(ILogger<SeedAppService> logger, ITokenAppService tokenAppService,
        IBidAppService bidAppService, ISeedProvider seedProvider,
        ISymbolIconAppService symbolIconAppService, IObjectMapper objectMapper, IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus, ISynchronizeTransactionProvider synchronizeTransactionProvider,
        INESTRepository<TsmSeedSymbolIndex, string> tsmSeedSymbolIndexRepository,
        INESTRepository<SeedPriceIndex, string> seedPriceIndexRepository,
        INESTRepository<UniqueSeedPriceIndex, string> uniqueSeedPriceIndexRepository,
        IOptionsMonitor<TransactionFeeOption> optionsMonitor,
        ITsmSeedProvider tsmSeedProvider,
        IGraphQLProvider graphQlProvider,
        INFTListingProvider nftListingProvider,
        INFTOfferProvider nftOfferProvider,
        IUserBalanceProvider userBalanceProvider,
        INFTDealInfoProvider dealInfoProvider,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IDistributedCache<PriceInfo> distributedCache,
        IUserAppService userAppService,
        IOptionsMonitor<SeedRenewOptions> seedRenewOptionsMonitor,
        IBlockchainClientFactory<AElfClient> blockchainClientFactory,
        IOptionsMonitor<ChainOptions> chainOptionsMonitor
        )
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
        _distributedCache = distributedCache;
        _graphQlProvider = graphQlProvider;
        _nftListingProvider = nftListingProvider;
        _nftOfferProvider = nftOfferProvider;
        _userBalanceProvider = userBalanceProvider;
        _dealInfoProvider = dealInfoProvider;
        _userAppService = userAppService;
        _seedRenewOptionsMonitor = seedRenewOptionsMonitor;
        _blockchainClientFactory = blockchainClientFactory;
        _chainOptionsMonitor = chainOptionsMonitor;

    }

    [ExceptionHandler(typeof(Exception),
        Message = "SeedAppService.CreateSeedAsync An error occurred during synchronous job.",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new[] { "input" }
    )]
    public virtual async Task<CreateSeedResultDto> CreateSeedAsync(CreateSeedDto input)
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

            var shouldPriceMinMustTokenPriceQuery =
                new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMinMustTokenPriceQuery.Add(q => q.Range(i => i.Field(f => f.TokenPrice.Amount).GreaterThan(0)));
            shouldPriceMinMustTokenPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TokenPrice.Amount).GreaterThanOrEquals(input.PriceMin)));
            var shouldPriceMinMustTopBidPriceQuery =
                new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMinMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).GreaterThan(0)));
            shouldPriceMinMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).GreaterThanOrEquals(input.PriceMin)));

            shouldPriceMinQuery.Add(q => q.Bool(b => b.Must(shouldPriceMinMustTokenPriceQuery)));
            shouldPriceMinQuery.Add(q => q.Bool(b => b.Must(shouldPriceMinMustTopBidPriceQuery)));

            mustQuery.Add(q => q.Bool(b => b.Should(shouldPriceMinQuery)));
        }

        if (input.PriceMax != null)
        {
            var shouldPriceMaxQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();

            var shouldPriceMaxMustTokenPriceQuery =
                new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMaxMustTokenPriceQuery.Add(q => q.Range(i => i.Field(f => f.TokenPrice.Amount).GreaterThan(0)));
            shouldPriceMaxMustTokenPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TokenPrice.Amount).LessThanOrEquals(input.PriceMax)));
            var shouldPriceMaxMustTopBidPriceQuery =
                new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
            shouldPriceMaxMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).GreaterThan(0)));
            shouldPriceMaxMustTopBidPriceQuery.Add(q =>
                q.Range(i => i.Field(f => f.TopBidPrice.Amount).LessThanOrEquals(input.PriceMax)));

            shouldPriceMaxQuery.Add(q => q.Bool(b => b.Must(shouldPriceMaxMustTokenPriceQuery)));
            shouldPriceMaxQuery.Add(q => q.Bool(b => b.Must(shouldPriceMaxMustTopBidPriceQuery)));

            mustQuery.Add(q => q.Bool(b => b.Should(shouldPriceMaxQuery)));
        }

        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Status).Terms(SeedStatus.AVALIABLE, SeedStatus.UNREGISTERED)));
        mustQuery.Add(q => q.Exists(i => i.Field(f => f.ChainId)));

        var shouldQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        shouldQuery.Add(q => q.Term(i => i.Field(f => f.AuctionEndTime).Value(0)));
        shouldQuery.Add(q => q.Range(i =>
            i.Field(f => f.AuctionEndTime).GreaterThan(DateTime.Now.ToUniversalTime().ToTimestamp().Seconds)));
        mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));

        var mustNotQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustNotQuery.Add(q => q.Term(i => i.Field(f => f.AuctionStatus).Value(AuctionFinishType.Finished)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));

        SortDescriptor<TsmSeedSymbolIndex> sortDescriptor = new SortDescriptor<TsmSeedSymbolIndex>();
        sortDescriptor.Descending(a => a.RankingWeight);
        sortDescriptor.Ascending(a => a.Symbol);
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

        var countResponse = await _tsmSeedSymbolIndexRepository.CountAsync(Filter);
        var totalCount = 0L;
        if (countResponse != null)
        {
            totalCount = countResponse.Count;
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

        var minDollarPriceMarkup =
            await _tokenAppService.GetCurrentDollarPriceAsync(SymbolHelper.CoinGeckoELF(), minElfPriceMarkup);

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

    [ExceptionHandler(typeof(Exception),
        Message = "SeedAppService.SearchSeedInfoAsync Search Seed info error.",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new[] { "input" }
    )]
    public virtual async Task<SeedDto> SearchSeedInfoAsync(SearchSeedInput input)
    {
        input.Symbol = input.Symbol.ToUpper();
        AssertHelper.IsTrue(AllType.Contains(input.TokenType), "Invalid symbol type");
        AssertHelper.IsTrue(input.TokenType == TokenType.FT.ToString()
            ? SymbolHelper.MatchSymbolPattern(input.Symbol)
            : SymbolHelper.MatchNFTPrefix(input.Symbol), "Invalid symbol pattern");

        //Get seed info from indexer
        var seedInfoDto = await _seedProvider.SearchSeedInfoAsync(input);
        if (seedInfoDto == null)
        {
            return null;
        }

        //Get token price from tsm seed symbol index
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(seedInfoDto.Symbol)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetAsync(Filter);
        var cacheKey = GetPopularTokenPriceCacheKey(input.Symbol);
        if (tsmSeedSymbolIndex != null && tsmSeedSymbolIndex.TokenPrice != null)
        {
            seedInfoDto.TokenPrice = new PriceInfo()
            {
                Amount = Convert.ToInt64(tsmSeedSymbolIndex.TokenPrice.Amount),
                Symbol = tsmSeedSymbolIndex.TokenPrice.Symbol
            };
            var cache = await _distributedCache.GetAsync(cacheKey);
            if (cache == null)
            {
                var priceInfo = new PriceInfo
                {
                    Amount = Convert.ToInt64(tsmSeedSymbolIndex.TokenPrice.Amount),
                    Symbol = NFTMarketServerConsts.AElfNativeTokenSymbol
                };
                _logger.LogInformation("[SeedAppService][SearchSeedInfoAsync] set seed price symbol: {symbol} {amount}",
                    input.Symbol, (tsmSeedSymbolIndex.TokenPrice.Amount));
                await _distributedCache.SetAsync(cacheKey, priceInfo, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(ExpirationDays)
                });
            }
        }

        if (seedInfoDto.TokenPrice == null || seedInfoDto.TokenPrice.Amount == 0)
        {
            _logger.LogInformation("[SeedAppService][SearchSeedInfoAsync] seed price 4 symbol: {symbol}", input.Symbol);
            var priceData = await _distributedCache.GetAsync(cacheKey);
            if (null != priceData)
            {
                seedInfoDto.TokenPrice = priceData;
            }
            else
            {
                _logger.LogInformation("[SeedAppService][SearchSeedInfoAsync] seed price 0 symbol: {symbol}",
                    input.Symbol);
            }
        }

        //recalculate token price for unique seed
        if (seedInfoDto.Status == SeedStatus.AVALIABLE &&
            (seedInfoDto.SeedType == SeedType.Regular || seedInfoDto.SeedType == SeedType.UNIQUE))
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
                    await _tokenAppService.GetCurrentDollarPriceAsync(seedDto.TopBidPrice.Symbol,
                        seedDto.TopBidPrice.Amount);
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
        var cacheKey = GetPopularTokenPriceCacheKey(input.Symbol);
        if (tsmSeedSymbolIndex != null && tsmSeedSymbolIndex.TokenPrice != null)
        {
            seedInfoDto.TokenPrice = new PriceInfo()
            {
                Amount = Convert.ToInt64(tsmSeedSymbolIndex.TokenPrice.Amount),
                Symbol = tsmSeedSymbolIndex.TokenPrice.Symbol
            };
            var cache = await _distributedCache.GetAsync(cacheKey);
            if (cache == null)
            {
                var priceInfo = new PriceInfo
                {
                    Amount = Convert.ToInt64(tsmSeedSymbolIndex.TokenPrice.Amount),
                    Symbol = NFTMarketServerConsts.AElfNativeTokenSymbol
                };
                _logger.LogInformation("[SeedAooService][GetSeedInfoAsync] set seed price symbol: {symbol} {amount}",
                    input.Symbol, (tsmSeedSymbolIndex.TokenPrice.Amount));
                await _distributedCache.SetAsync(cacheKey, priceInfo, new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(ExpirationDays)
                });
            }
        }

        if (seedInfoDto.TokenPrice == null || seedInfoDto.TokenPrice.Amount == 0)
        {
            _logger.LogInformation("[SeedAooService][GetSeedInfoAsync] seed price 4 symbol: {symbol}", input.Symbol);
            var priceData = await _distributedCache.GetAsync(cacheKey);
            if (null != priceData)
            {
                seedInfoDto.TokenPrice = priceData;
            }
            else
            {
                _logger.LogInformation("[SeedAooService][GetSeedInfoAsync] seed price 0 symbol: {symbol}",
                    input.Symbol);
            }
        }

        return await ConvertSeedDtoAsync(seedInfoDto);
    }

    public async Task<PagedResultDto<SeedDto>> MySeedAsync(MySeedInput input)
    {
        AssertHelper.NotNull(input.Address, "Address empty");
        input.Address = input.Address.Where(addr => addr.NotNullOrEmpty()).ToList();
        AssertHelper.NotEmpty(input.Address, "Address invalid");

        var seedInfoDto = new MySeedDto();

        seedInfoDto = await DoMySeedWithFilterAsync(input);

        var mySeedDto = new PagedResultDto<SeedDto>()
        {
            TotalCount = seedInfoDto.TotalRecordCount,
            Items = seedInfoDto.SeedDtoList
        };
        return mySeedDto;
    }

    private async Task<MySeedDto> DoMySeedAsync(
        MySeedInput input)
    {
        if (input.Address.IsNullOrEmpty())
        {
            return null;
        }

        var target = await _seedSymbolIndexRepository.GetAsync("AELF-SEED-3022");
        _logger.LogInformation("AELF-SEED-3022 {A}", JsonConvert.SerializeObject(target));
        var target2 = await _seedSymbolIndexRepository.GetAsync("tDVV-SEED-3022");
        _logger.LogInformation("tDVV-SEED-3022 {A}", JsonConvert.SerializeObject(target2));

        
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        
        if (input.TokenType != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenType).Value(input.TokenType)));
        }

        if (!string.IsNullOrEmpty(input.ChainId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        BuildForSeedStatusNull(input,shouldQuery);
        // if (input.Status == null)
        // {
        //     BuildForSeedStatusNull(input,shouldQuery);
        // }else {
        //     BuildForSeedStatusNoNull(input, shouldQuery, mustQuery);
        // }
        
        if (shouldQuery.Any())
        {
            mustQuery.Add(q =>
                q.Bool(b => b.Should(shouldQuery)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var mySeedSymbolIndex = await _seedSymbolIndexRepository.GetListAsync(Filter, null, sortExp: o => o.SeedExpTimeSecond,
            SortOrder.Descending, input.MaxResultCount, input.SkipCount);
        var seedInfoDtos = new List<SeedDto>();
        if (mySeedSymbolIndex.Item1 > 0)
        {
            foreach (var seedSymbolIndex in mySeedSymbolIndex.Item2)
            {
                var seedListDto = new SeedDto();
                seedListDto.SeedSymbol = seedSymbolIndex.Symbol;
                seedListDto.ChainId = seedSymbolIndex.ChainId;
                seedListDto.SeedName = seedSymbolIndex.TokenName;
                seedListDto.Id = IdGenerateHelper.GetTsmSeedSymbolId(seedSymbolIndex.ChainId,seedSymbolIndex.SeedOwnedSymbol);
                seedListDto.Symbol = seedSymbolIndex.SeedOwnedSymbol;
                seedListDto.ExpireTime = DateTimeHelper.ToUnixTimeMilliseconds(seedSymbolIndex.SeedExpTime);
                seedListDto.TokenType = EnumHelper.ToEnumString(seedSymbolIndex.TokenType);
                seedListDto.Status = seedSymbolIndex.SeedStatus ?? SeedStatus.UNREGISTERED;
                if (seedSymbolIndex.ExternalInfoDictionary != null)
                {
                    var key = EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTImageUrl);
                    seedListDto.SeedImage = seedSymbolIndex.ExternalInfoDictionary.Where(kv => kv.Key.Equals(key))
                        .Select(kv => kv.Value)
                        .FirstOrDefault("");
                }
                seedInfoDtos.Add(seedListDto);
            }
        }

        return new MySeedDto()
        {
            TotalRecordCount = mySeedSymbolIndex.Item1,
            SeedDtoList = seedInfoDtos
        };
    }

    private async Task<PriceInfo> GetSeedPriceAsync(string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TsmSeedSymbolIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol)));
        //mustQuery.Add(q => q.Term(i => i.Field(f => f.IsBurned).Value(false)));

        QueryContainer Filter(QueryContainerDescriptor<TsmSeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var tsmSeedSymbolIndex = await _tsmSeedSymbolIndexRepository.GetAsync(Filter);
        _logger.LogInformation("GetSeedPriceAsync symbol:{symbol}, tsmSeedSymbolIndex:{seedIndex}",symbol,JsonConvert.SerializeObject(tsmSeedSymbolIndex));
        if (tsmSeedSymbolIndex == null || tsmSeedSymbolIndex.TokenPrice == null || tsmSeedSymbolIndex.TokenPrice.Symbol.IsNullOrEmpty())
        {
            return null;
        }
        var priceInfo = new PriceInfo
        {
            Amount = Convert.ToInt64(tsmSeedSymbolIndex.TokenPrice.Amount),
            Symbol = NFTMarketServerConsts.AElfNativeTokenSymbol
        };
        return priceInfo;
    }

    private async Task<MySeedDto> DoMySeedWithFilterAsync(
        MySeedInput input)
    {
        if (input.Address.IsNullOrEmpty())
        {
            return null;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        if (input.TokenType != null)
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.TokenType).Value(input.TokenType)));
        }

        if (!string.IsNullOrEmpty(input.ChainId))
        {
            mustQuery.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(input.ChainId)));
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();

        if (input.Status == null)
        {
            BuildForSeedStatusNull(input, shouldQuery);
        }
        else
        {
            BuildForSeedStatusNoNull(input, shouldQuery, mustQuery);
        }

        if (shouldQuery.Any())
        {
            mustQuery.Add(q =>
                q.Bool(b => b.Should(shouldQuery)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f)
            => f.Bool(b => b.Must(mustQuery));
        
        var mySeedSymbolIndex = await _seedSymbolIndexRepository.GetListAsync(Filter, null,
            sortExp: o => o.SeedExpTimeSecond,
            SortOrder.Descending, CommonConstant.IntTenThousand);
        var seedInfoDtos = new List<SeedDto>();
        var nowSeconds = TimeHelper.ToUtcSeconds(DateTime.UtcNow);
        var totalRecordCount = mySeedSymbolIndex.Item1;
        if (mySeedSymbolIndex.Item1 > 0)
        {
            var filterItemList = new List<SeedSymbolIndex>();
            if (input.Status == SeedStatus.NOTSUPPORT)
            {
                var temList = mySeedSymbolIndex.Item2
                    .GroupBy(e => e.SeedOwnedSymbol)
                    .Select(g => g.OrderByDescending(e => e.SeedExpTimeSecond).First())
                    .OrderByDescending(e => e.SeedExpTimeSecond)
                    .Where(i => i.SeedExpTimeSecond < nowSeconds)
                    .ToList();
                totalRecordCount = temList.Count;
                filterItemList = temList
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .ToList();
            }
            else if (input.Status == SeedStatus.UNREGISTERED)
            {
                var temList = mySeedSymbolIndex.Item2
                    .GroupBy(e => e.SeedOwnedSymbol)
                    .Select(g => g.OrderByDescending(e => e.SeedExpTimeSecond).First())
                    .OrderByDescending(e => e.SeedExpTimeSecond)
                    .Where(i => i.SeedExpTimeSecond > nowSeconds)
                    .ToList();
                totalRecordCount = temList.Count;
                filterItemList = temList
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .ToList();
            }else
            {
                var temList = mySeedSymbolIndex.Item2
                    .GroupBy(e => e.SeedOwnedSymbol)
                    .Select(g => g.OrderByDescending(e => e.SeedExpTimeSecond).First())
                    .OrderByDescending(e => e.SeedExpTimeSecond)
                    .ToList();
                totalRecordCount = temList.Count;
                filterItemList = temList
                    .Skip(input.SkipCount)
                    .Take(input.MaxResultCount)
                    .ToList();
            }
            
            foreach (var seedSymbolIndex in filterItemList)
            {
                var seedListDto = new SeedDto();
                seedListDto.SeedSymbol = seedSymbolIndex.Symbol;
                seedListDto.ChainId = seedSymbolIndex.ChainId;
                seedListDto.SeedName = seedSymbolIndex.TokenName;
                seedListDto.Id =
                    IdGenerateHelper.GetTsmSeedSymbolId(seedSymbolIndex.ChainId, seedSymbolIndex.SeedOwnedSymbol);
                seedListDto.Symbol = seedSymbolIndex.SeedOwnedSymbol;
                seedListDto.ExpireTime = DateTimeHelper.ToUnixTimeMilliseconds(seedSymbolIndex.SeedExpTime);
                seedListDto.TokenType = EnumHelper.ToEnumString(seedSymbolIndex.TokenType);
                seedListDto.Status = seedSymbolIndex.SeedStatus ?? SeedStatus.UNREGISTERED;
                seedListDto.SeedType = seedSymbolIndex.SeedType;
                
                //seedListDto.TokenPrice.Symbol = seedSymbolIndex.PriceSymbol;
                //seedListDto.TokenPrice.Amount = seedSymbolIndex.Price; 
                
                var price = await GetSeedPriceAsync(seedListDto.Symbol);
                if (price != null)
                {
                    var tokenPrice = new TokenPriceDto()
                    {
                        Symbol = price.Symbol,
                        Amount = price.Amount
                    };
                    seedListDto.TokenPrice = tokenPrice;
                }

                if (seedSymbolIndex.ExternalInfoDictionary != null)
                {
                    var key = EnumDescriptionHelper.GetEnumDescription(TokenCreatedExternalInfoEnum.NFTImageUrl);
                    seedListDto.SeedImage = seedSymbolIndex.ExternalInfoDictionary.Where(kv => kv.Key.Equals(key))
                        .Select(kv => kv.Value)
                        .FirstOrDefault("");
                }

                seedInfoDtos.Add(seedListDto);
            }
        }

        return new MySeedDto()
        {
            TotalRecordCount = totalRecordCount,
            SeedDtoList = seedInfoDtos
        };
    }

    
    private static void BuildForSeedStatusNull(MySeedInput input,List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> shouldQuery)
    {
        input?.Address
            .ForEach(address =>
            {
                var parts = address.Split(CommonConstant.Underscore);
                if (parts.Length < 2)
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(address)) &&
                        q.Term(i =>
                            i.Field(f => f.SeedStatus).Value(SeedStatus.REGISTERED)));
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(address)) &&
                        q.Term(i =>
                            i.Field(f => f.IsDeleteFlag).Value(false)));
                }
                else
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(parts[1])) &&
                        q.Term(i => i.Field(f => f.ChainId).Value(parts.Last())) &&
                        q.Term(i =>
                            i.Field(f => f.SeedStatus).Value(SeedStatus.REGISTERED)));
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(parts[1])) &&
                        q.Term(i => i.Field(f => f.ChainId).Value(parts.Last())) &&
                        q.Term(i =>
                            i.Field(f => f.IsDeleteFlag).Value(false)));
                }
            });
    }

    private static void BuildForSeedStatusNoNull(MySeedInput input,
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> shouldQuery,
        List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>> mustQuery)
    {
        input?.Address
            .ForEach(address =>
            {
                var parts = address.Split(CommonConstant.Underscore);
                if (parts.Length < 2)
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(address)));
                }
                else
                {
                    shouldQuery.Add(q =>
                        q.Term(i => i.Field(f => f.IssuerTo).Value(parts[1])) &&
                        q.Term(i => i.Field(f => f.ChainId).Value(parts.Last())));
                }

                if (input.Status != SeedStatus.UNREGISTERED && input.Status != SeedStatus.NOTSUPPORT)
                {
                    mustQuery.Add(q => q.Term(i =>
                        i.Field(f => f.SeedStatus).Value(input.Status)));
                }

                if (input.Status != SeedStatus.REGISTERED)
                {
                    mustQuery.Add(q => q.Term(i =>
                        i.Field(f => f.IsDeleteFlag).Value(false)));
                }
            });
    }

    public async Task<TransactionFeeDto> GetTransactionFeeAsync(string symbol)
    {
        var transactionFeeOption = _optionsMonitor.CurrentValue;
        var marketData = await _tokenAppService.GetTokenMarketDataAsync(SymbolHelper.CoinGeckoELF(), null);
        var result = decimal.Multiply(transactionFeeOption.TransactionFee, marketData.Price);
        var roundedResult = Math.Round(result, transactionFeeOption.Decimals);

        var collectionLoyaltyRates = transactionFeeOption.CollectionLoyaltyRates;
        decimal creatorLoyaltyRate = 0;
        if (!collectionLoyaltyRates.IsNullOrEmpty() &&
            collectionLoyaltyRates.FirstOrDefault(x => x.Symbol == symbol) != null)
        {
            creatorLoyaltyRate = collectionLoyaltyRates.FirstOrDefault(x => x.Symbol == symbol).Rate;
        }


        return new TransactionFeeDto
        {
            TransactionFee = transactionFeeOption.TransactionFee,
            TransactionFeeOfUsd = roundedResult,
            ForestServiceRate = transactionFeeOption.ForestServiceRate,
            CreatorLoyaltyRate = creatorLoyaltyRate,
            AIImageFee = transactionFeeOption.AIImageFee
        };
    }

    public async Task AddOrUpdateTsmSeedInfoAsync(SeedDto seedDto)
    {
        if (!seedDto.SeedSymbol.IsNullOrEmpty())
        {
            await UpdateSeedSymbolAsync(IdGenerateHelper.GetSeedMainChainChangeId(seedDto.ChainId, seedDto.SeedSymbol),
                seedDto.ChainId);
        }
        else
        {
            _logger.LogDebug("SeedSymbol is null Symbol={A} SeedType={B} chainId={C}", seedDto.Symbol,
                seedDto.SeedType.ToString(), seedDto.ChainId);
        }
        
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
                        otherChainTsmSeedSymbolIndex.TokenPrice == null
                            ? ""
                            : otherChainTsmSeedSymbolIndex.TokenPrice.Amount,
                        otherChainTsmSeedSymbolIndex.Id);
                    tsmSeedSymbolIndex.TokenPrice = otherChainTsmSeedSymbolIndex.TokenPrice;
                }

                tsmSeedSymbolIndex.RankingWeight = otherChainTsmSeedSymbolIndex.RankingWeight;
            }
        }

        await _tsmSeedSymbolIndexRepository.AddOrUpdateAsync(tsmSeedSymbolIndex);
    }

    public async Task UpdateSeedSymbolAsync(string seedSymbolIndexId, string chainId)
    {
        var seedSymbol = await _graphQlProvider.GetSyncSeedSymbolRecordAsync(seedSymbolIndexId, chainId);

        if (seedSymbol == null)
        {
            _logger.LogError(
                "AddOrUpdateSeedSymbolAsync fromNFTInfo and localNFTInfo are null! seedSymbolIndexId={A} chainId={B}",
                seedSymbolIndexId, chainId);
            return;
        }

        _logger.LogDebug("AddOrUpdateSeedSymbolAsync seedSymbolIndexId={A} chainId={B}", seedSymbolIndexId, chainId);

        await UpdateSeedSymbolOtherInfoAsync(seedSymbol);
    }
    
    public async Task AddOrUpdateSeedSymbolAsync(SeedSymbolIndex seedSymbol)
    {
        if (seedSymbol.ChainId != CommonConstant.MainChainId)
        {
            var id = IdGenerateHelper.GetSeedSymbolId(GetDefaultSideChainId(), seedSymbol.Symbol);
            var mainSeedSymbolIndex = await _seedSymbolIndexRepository.GetAsync(id);
            if (mainSeedSymbolIndex != null && mainSeedSymbolIndex.SeedExpTimeSecond > seedSymbol.SeedExpTimeSecond)
            {
                _logger.LogDebug("update SeedExpTimeSecond from mainchain chainId={A} symbol={B}", seedSymbol.ChainId,
                    seedSymbol.Symbol);
                seedSymbol.SeedExpTimeSecond = mainSeedSymbolIndex.SeedExpTimeSecond;
                seedSymbol.SeedExpTime = mainSeedSymbolIndex.SeedExpTime;
            }
        }
        
        seedSymbol.FuzzySymbol = seedSymbol.Symbol;
        seedSymbol.FuzzyTokenName = seedSymbol.TokenName;
        seedSymbol.FuzzyTokenId = SymbolHelper.GetTrailingNumber(seedSymbol.Symbol);
        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbol);
    }

    private string GetDefaultSideChainId()
    {
        var chainIds = _chainOptionsMonitor.CurrentValue.ChainInfos.Keys;
        foreach (var chainId in chainIds)
        {
            if (!chainId.Equals(CommonConstant.MainChainId))
            {
                return chainId;
            }
        }

        return "";
    }
    
    private async Task UpdateSeedSymbolOtherInfoAsync(SeedSymbolIndex seedSymbolIndex)
    {
        if (seedSymbolIndex.ChainId.Equals(CommonConstant.MainChainId))
        {
            return;
        }

        var mainChainSeedSymbol = await _graphQlProvider.GetSyncSeedSymbolRecordAsync(
            IdGenerateHelper.GetSeedSymbolId(seedSymbolIndex.Symbol, CommonConstant.MainChainId),
            CommonConstant.MainChainId);

        if (mainChainSeedSymbol != null && mainChainSeedSymbol.SeedExpTimeSecond > seedSymbolIndex.SeedExpTimeSecond)
        {
            _logger.LogDebug("UpdateSeedSymbolOtherInfoAsync modify SeedExpTimeSecond");
            seedSymbolIndex.SeedExpTimeSecond = mainChainSeedSymbol.SeedExpTimeSecond;
            seedSymbolIndex.SeedExpTime = mainChainSeedSymbol.SeedExpTime;
        }
        
        
        var getNftListingsDto = new GetNFTListingsDto
        {
            ChainId = seedSymbolIndex.ChainId,
            Symbol = seedSymbolIndex.Symbol,
            SkipCount = CommonConstant.IntZero,
            MaxResultCount = CommonConstant.IntOne
        };

        var listingDto = await _nftListingProvider.GetNFTListingsAsync(getNftListingsDto);
        if (listingDto != null && listingDto.TotalCount > CommonConstant.IntZero)
        {
            UpdateMinListingInfo(seedSymbolIndex, listingDto.Items[CommonConstant.IntZero]);
        }
        else
        {
            UpdateMinListingInfo(seedSymbolIndex, null);
        }

        var indexerNFTOffer = await _nftOfferProvider.GetMaxOfferInfoAsync(seedSymbolIndex.Id);
        _logger.LogDebug("UpdateSeedSymbolOtherInfoAsync seedSymbolIndex.Id={A} indexerNFTOffer.Id={B} offerIsNull={C}",
            seedSymbolIndex.Id,
            indexerNFTOffer?.Id, indexerNFTOffer == null);
        if (indexerNFTOffer != null && !indexerNFTOffer.Id.IsNullOrEmpty())
        {
            UpdateMaxOfferInfo(seedSymbolIndex, indexerNFTOffer);
        }
        else
        {
            UpdateMaxOfferInfo(seedSymbolIndex, null);
        }

        var getLatestDeal = new GetNftDealInfoDto()
        {
            Symbol = seedSymbolIndex.Symbol,
            ChainId = seedSymbolIndex.ChainId,
            SkipCount = CommonConstant.IntZero,
            MaxResultCount = CommonConstant.IntOne
        };
        var indexerNFTDealInfos = await _dealInfoProvider.GetDealInfosAsync(getLatestDeal);
        if (indexerNFTDealInfos != null && indexerNFTDealInfos.TotalRecordCount > CommonConstant.IntZero)
        {
            UpdatelatestDealInfo(seedSymbolIndex, indexerNFTDealInfos.IndexerNftDealList[CommonConstant.IntZero]);
        }
        else
        {
            UpdatelatestDealInfo(seedSymbolIndex, null);
        }

        var balanceInfo = await _userBalanceProvider.GetNFTBalanceInfoAsync(seedSymbolIndex.Id);
        if (balanceInfo != null)
        {
            seedSymbolIndex.RealOwner = balanceInfo.Owner;
            seedSymbolIndex.AllOwnerCount = balanceInfo.OwnerCount;
        }

        if (!seedSymbolIndex.HasListingFlag)
        {
            seedSymbolIndex.ListingPrice = CommonConstant.DefaultValueNone;
        }

        if (!seedSymbolIndex.HasOfferFlag)
        {
            seedSymbolIndex.MaxOfferPrice = CommonConstant.DefaultValueNone;
        }

        if (seedSymbolIndex.LatestDealPrice == CommonConstant.IntZero)
        {
            seedSymbolIndex.LatestDealPrice = CommonConstant.DefaultValueNone;
        }

        seedSymbolIndex.FuzzySymbol = seedSymbolIndex.Symbol;
        seedSymbolIndex.FuzzyTokenName = seedSymbolIndex.TokenName;
        seedSymbolIndex.FuzzyTokenId = SymbolHelper.GetTrailingNumber(seedSymbolIndex.Symbol);

        await _seedSymbolIndexRepository.AddOrUpdateAsync(seedSymbolIndex);
    }

    private bool UpdateMinListingInfo(SeedSymbolIndex seedSymbolIndex, IndexerNFTListingInfo listingDto)
    {
        _logger.LogDebug("Seed UpdateMinListingInfo nftInfoIndexId={A} listingDto={B}", seedSymbolIndex.Id,
            JsonConvert.SerializeObject(listingDto));
        if (listingDto == null && seedSymbolIndex.ListingId.IsNullOrEmpty())
        {
            if (seedSymbolIndex.HasListingFlag)
            {
                seedSymbolIndex.HasListingFlag = false;
                return true;
            }

            return false;
        }

        if (listingDto != null && listingDto.Id.Equals(seedSymbolIndex.ListingId))
        {
            return false;
        }

        if (listingDto != null)
        {
            seedSymbolIndex.ListingId = listingDto.Id;
            seedSymbolIndex.ListingPrice = listingDto.Prices;

            seedSymbolIndex.MinListingId = listingDto.Id;
            seedSymbolIndex.MinListingPrice = listingDto.Prices;
            seedSymbolIndex.MinListingExpireTime = listingDto.ExpireTime;

            seedSymbolIndex.ListingAddress = listingDto?.Owner;
            seedSymbolIndex.ListingQuantity = listingDto.RealQuantity;
            seedSymbolIndex.ListingEndTime = listingDto.ExpireTime;
            seedSymbolIndex.LatestListingTime = listingDto.StartTime;
            seedSymbolIndex.ListingToken =
                _objectMapper.Map<IndexerTokenInfo, TokenInfoIndex>(listingDto.PurchaseToken);
            seedSymbolIndex.HasListingFlag = listingDto.Prices > CommonConstant.IntZero;
        }
        else
        {
            seedSymbolIndex.ListingId = null;
            seedSymbolIndex.ListingPrice = -1;

            seedSymbolIndex.MinListingId = null;
            seedSymbolIndex.MinListingPrice = -1;
            seedSymbolIndex.MinListingExpireTime = DateTime.UtcNow;

            seedSymbolIndex.ListingAddress = null;
            seedSymbolIndex.ListingQuantity = 0;
            seedSymbolIndex.ListingEndTime = DateTime.UtcNow;
            seedSymbolIndex.LatestListingTime = DateTime.UtcNow;
            seedSymbolIndex.ListingToken = null;
            seedSymbolIndex.HasListingFlag = false;
        }

        return true;
    }

    private bool UpdateMaxOfferInfo(SeedSymbolIndex seedSymbolIndex, IndexerNFTOffer indexerNFTOffer)
    {
        if (indexerNFTOffer != null)
        {
            seedSymbolIndex.MaxOfferId = indexerNFTOffer.Id;
            seedSymbolIndex.MaxOfferPrice = indexerNFTOffer.Price;
            seedSymbolIndex.MaxOfferExpireTime = indexerNFTOffer.ExpireTime;
            seedSymbolIndex.OfferToken = new TokenInfoIndex
            {
                ChainId = indexerNFTOffer.PurchaseToken.ChainId,
                Symbol = indexerNFTOffer.PurchaseToken.Symbol,
                Decimals = Convert.ToInt32(indexerNFTOffer.PurchaseToken.Decimals),
                Prices = indexerNFTOffer.Price
            };
            seedSymbolIndex.HasOfferFlag = seedSymbolIndex.MaxOfferPrice > CommonConstant.IntZero;
        }
        else
        {
            seedSymbolIndex.MaxOfferId = null;
            seedSymbolIndex.MaxOfferPrice = -1;
            seedSymbolIndex.MaxOfferExpireTime = DateTime.UtcNow;
            seedSymbolIndex.OfferToken = null;
            seedSymbolIndex.HasOfferFlag = false;
        }

        return true;
    }

    private bool UpdatelatestDealInfo(SeedSymbolIndex seedSymbolIndex, IndexerNFTDealInfo indexerNFTDeal)
    {
        if (indexerNFTDeal == null && seedSymbolIndex.LatestDealId.IsNullOrEmpty())
        {
            return false;
        }

        if (indexerNFTDeal != null && indexerNFTDeal.Id.Equals(seedSymbolIndex.LatestDealId))
        {
            return false;
        }

        if (indexerNFTDeal != null)
        {
            seedSymbolIndex.LatestDealId = indexerNFTDeal.Id;
            seedSymbolIndex.LatestDealPrice = FTHelper.GetRealELFAmount(indexerNFTDeal.PurchaseAmount);
            seedSymbolIndex.LatestListingTime = indexerNFTDeal.DealTime;
            seedSymbolIndex.LatestDealToken = new TokenInfoIndex
            {
                ChainId = indexerNFTDeal.ChainId,
                Symbol = indexerNFTDeal.PurchaseSymbol,
                Decimals = CommonConstant.Coin_ELF_Decimals,
                Prices = indexerNFTDeal.PurchaseAmount
            };
            seedSymbolIndex.HasOfferFlag = seedSymbolIndex.MaxOfferPrice > CommonConstant.IntZero;
        }
        else
        {
            seedSymbolIndex.LatestDealId = null;
            seedSymbolIndex.LatestDealPrice = -1;
            seedSymbolIndex.LatestListingTime = DateTime.UtcNow;
            seedSymbolIndex.LatestDealToken = null;
        }

        return true;
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
                _logger.LogInformation("UpdateSeedRankingWeightAsync {0} successfully, NewWeight {1}",
                    tsmSeedSymbolIndex.Id,
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
        seedRankingWeightDtoList = seedRankingWeightDtoList.DistinctBy(s => s.Symbol)
            .OrderByDescending(o => o.RankingWeight)
            .OrderByDescending(o => o.Symbol)
            .ToList();
        return new PagedResultDto<SeedRankingWeightDto>()
        {
            TotalCount = seedRankingWeightDtoList.Count,
            Items = seedRankingWeightDtoList
        };
    }

    private string GetPopularTokenPriceCacheKey(string symbol)
    {
        return $"popular:price:{symbol}";
    }
    
    public async Task<SeedRenewParamDto> GetSpecialSeedRenewParamAsync(SpecialSeedRenewDto input)
    {
        /*var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
        if (currentUserAddress != input.BuyerAddress)
        {
            throw new Exception("Login address and parameter buyerAddress are inconsistent");
        }*/
        
        var queryList = await _graphQlProvider.GetTsmSeedBySymbolsAsync("", new List<string>(){input.SeedSymbol});
        _logger.LogInformation(
            "GetSpecialSeedRenewParamAsync.GetTsmSeedBySymbolsAsync symbol: {symbol} queryList:{querylist}",input.SeedSymbol, JsonConvert.SerializeObject(queryList));
        if (queryList.IsNullOrEmpty())
        {
            throw new Exception("can not find last seed price");
        }


        var lastPrice = queryList.FirstOrDefault(x => x.TokenPrice.Amount > 0);
        if (lastPrice.TokenPrice.Symbol.IsNullOrEmpty())
        {
            throw new Exception("can not find last seed price");
        }

        var priceSymbol = lastPrice.TokenPrice.Symbol;
        var priceAmount = lastPrice.TokenPrice.Amount;
        
        var opTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
        var requestStr = string.Concat(input.BuyerAddress, input.SeedSymbol, priceSymbol, (long)priceAmount);
        var requestHash =  BuildRequestHash(string.Concat(requestStr, opTime));
        
        var response = new SeedRenewParamDto()
        {
            Buyer = input.BuyerAddress,
            SeedSymbol = input.SeedSymbol,
            PriceSymbol = priceSymbol,
            PriceAmount = (long)priceAmount,
            OpTime = opTime,
            RequestHash = requestHash
        };
        return response;
    }
    
    private string BuildRequestHash(string request)
    {
        var hashVerifyKey = _seedRenewOptionsMonitor.CurrentValue.HashVerifyKey;
        if (hashVerifyKey.IsNullOrEmpty())
        {
            throw new Exception("have not config seed renew HashVerifyKey");
        }
        var requestHash = HashHelper.ComputeFrom(string.Concat(request, hashVerifyKey));
        _logger.LogInformation("seed renew request:{A} key:{B} hash:{C}",request,hashVerifyKey, requestHash);
        return requestHash.ToHex();
    }
    
    public async Task<bool> BidSeedRenew(AuctionInfoDto input)
    {
        var seedSymbol = input.SeedSymbol;
        var bidFinishTime = input.EndTime;
        var renewSwitch = _seedRenewOptionsMonitor.CurrentValue.RenewSwitch;
        if (!renewSwitch)
        {
            _logger.LogInformation("Bid Seed renew switch is close");
            return true;
        }
        //get request param
        var opTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)/1000;
        var requestStr = string.Concat(seedSymbol, bidFinishTime, opTime);
        var requestHash =  BuildRequestHash(string.Concat(requestStr));
        
        //send tx
        var chainId = _seedRenewOptionsMonitor.CurrentValue.RenewChainId;
        var contractAddress = _seedRenewOptionsMonitor.CurrentValue.SymbolRegistarContractMainChainAddress;
        var privateKey = _seedRenewOptionsMonitor.CurrentValue.PrivateKey;
        var client = _blockchainClientFactory.GetClient(chainId);
        var renewInput = new BidFinishSeedRenewInput()
        {
            SeedSymbol = seedSymbol,
            BidFinishTime = bidFinishTime,
            OpTime = opTime,
            RequestHash = requestHash
        };
        var seedRenewRaw =
            await GenerateRawTransaction(client, "BidFinishSeedRenew", renewInput,
                contractAddress, privateKey);
        _logger.LogInformation(
            "BidSeedRenew create raw SeedSymbol:{A} BidFinishTime:{B} OpTime:{C} RequestHash:{D} seedRenewRaw:{E}", renewInput.SeedSymbol,
            renewInput.BidFinishTime, renewInput.OpTime, renewInput.RequestHash, seedRenewRaw);
        
        var result = await client.SendTransactionAsync(new SendTransactionInput()
            { RawTransaction = seedRenewRaw });
        await Task.Delay(3000);
        
        var transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
        var times = 0;
        while ((transactionResult.Status is "PENDING" or "NOTEXISTED") && times < 60)
        {
            times++;
            await Task.Delay(1000);
            transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
        }

        bool isSuccess = transactionResult.Status == "MINED";
        _logger.LogInformation(
            "BidSeedRenew sendTx is {isSuccess} SeedSymbol:{A} BidFinishTime:{B} OpTime:{C} RequestHash:{D} tx:{E}", isSuccess, renewInput.SeedSymbol,
            renewInput.BidFinishTime, renewInput.OpTime, renewInput.RequestHash, JsonConvert.SerializeObject(transactionResult));
        return isSuccess;
    }
    private async Task<string> GenerateRawTransaction(AElfClient client, string methodName, IMessage param,
        string contractAddress, string privateKey)
    {
        try
        {
            return client.SignTransaction(privateKey, await client.GenerateTransactionAsync(
                    client.GetAddressFromPrivateKey(privateKey), contractAddress, methodName, param))
                .ToByteArray().ToHex();
        }
        catch (Exception e)
        {
            throw e;
        }

        
    }
}
public class Excepton : Exception
{
}