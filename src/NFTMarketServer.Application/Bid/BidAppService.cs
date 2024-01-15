using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.BId.Index;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Bid;

public class BidAppService : NFTMarketServerAppService, IBidAppService
{
    private readonly INESTRepository<SymbolBidInfoIndex, string> _symbolBidInfoIndexRepository;
    private readonly INESTRepository<SymbolAuctionInfoIndex, string> _symbolAuctionInfoIndexRepository;
    private readonly IBus _bus;
    private readonly ITokenAppService _tokenAppService;
    private const decimal MinMarkupDenominator = 10000;
    private readonly ILogger<BidAppService> _logger;
    private readonly IChainAppService _chainAppService;

    public BidAppService(
        INESTRepository<SymbolBidInfoIndex, string> symbolBidInfoIndexRepository,
        INESTRepository<SymbolAuctionInfoIndex, string> symbolAuctionInfoIndexRepository,
        IBus bus, ITokenAppService tokenAppService, ILogger<BidAppService> logger, IChainAppService chainAppService)
    {
        _symbolBidInfoIndexRepository = symbolBidInfoIndexRepository;
        _symbolAuctionInfoIndexRepository = symbolAuctionInfoIndexRepository;
        _bus = bus;
        _tokenAppService = tokenAppService;
        _logger = logger;
        _chainAppService = chainAppService;
    }

    public async Task<PagedResultDto<BidInfoDto>> GetSymbolBidInfoListAsync(GetSymbolBidInfoListRequestDto input)
    {
        if (string.IsNullOrEmpty(input.SeedSymbol))
        {
            return new PagedResultDto<BidInfoDto>()
            {
                Items = new List<BidInfoDto>(),
                TotalCount = 0
            };
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolBidInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(input.SeedSymbol.ToUpper())));
        QueryContainer Filter(QueryContainerDescriptor<SymbolBidInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<SymbolBidInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.BidTime));

        var list = await _symbolBidInfoIndexRepository.GetSortListAsync(Filter,
            sortFunc: sorting,
            limit: input.MaxResultCount == 0 ? BidConsts.DefaultListSize : input.MaxResultCount > BidConsts.MaxListSize ? BidConsts.MaxListSize : input.MaxResultCount,
            skip: input.SkipCount);

        var totalCount = await _symbolBidInfoIndexRepository.CountAsync(Filter);

        if (list.Item2.IsNullOrEmpty())
        {
            return null;
        }

        var bidInfoDtoList = ObjectMapper.Map<List<SymbolBidInfoIndex>, List<BidInfoDto>>(list.Item2);
        
        var symbolAuctionInfoAsync = await GetSymbolAuctionInfoAsync(input.SeedSymbol);
        
        foreach (var bidInfoDto in bidInfoDtoList)
        {
            var price = Convert.ToDecimal(bidInfoDto.PriceAmount);
            var realElfAmount = FTHelper.GetRealELFAmount(price);
            var usdPrice =
                await _tokenAppService.GetCurrentDollarPriceAsync(bidInfoDto.PriceSymbol, realElfAmount);
            bidInfoDto.PriceUsdAmount = usdPrice;
            bidInfoDto.PriceUsdSymbol = SeedConstants.UsdSymbol;
            if (!bidInfoDto.Bidder.IsNullOrEmpty())
            {
                var chainId = "tDVV";
                var chainIds = await _chainAppService.GetListAsync();
                if (chainIds != null && chainIds.Length > 1)
                {
                    chainId = chainIds[1];
                }

                bidInfoDto.Bidder = "ELF_" + bidInfoDto.Bidder + "_" + chainId;
            }

            if (symbolAuctionInfoAsync != null)
            {
                var elfAmount = FTHelper.GetRealELFAmount(bidInfoDto.PriceAmount);
                var minElfPriceMarkup = elfAmount * (symbolAuctionInfoAsync.MinMarkup / MinMarkupDenominator);
                var minDollarPriceMarkup = await _tokenAppService.GetCurrentDollarPriceAsync(bidInfoDto.PriceSymbol, minElfPriceMarkup);
                bidInfoDto.MinElfPriceMarkup = minElfPriceMarkup;
                bidInfoDto.MinDollarPriceMarkup = minDollarPriceMarkup;
                bidInfoDto.CalculatorMinMarkup = symbolAuctionInfoAsync.MinMarkup / MinMarkupDenominator;
            }


        }

        return new PagedResultDto<BidInfoDto>()
        {
            Items = bidInfoDtoList,
            TotalCount = totalCount.Count
        };

    }


    public async Task<BidInfoDto> GetSymbolBidInfoAsync(string symbol, string transactionHash)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolBidInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol.ToUpper())));

        mustQuery.Add(q => q.Term(i => i.Field(f => f.TransactionHash).Value(transactionHash)));

        QueryContainer Filter(QueryContainerDescriptor<SymbolBidInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var auctionInfo = await _symbolBidInfoIndexRepository.GetAsync(Filter);

        return auctionInfo != null ? ObjectMapper.Map<SymbolBidInfoIndex, BidInfoDto>(auctionInfo) : null;
    }

    public async Task<List<AuctionInfoDto>> GetSymbolAuctionInfoListAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol.ToUpper())));

        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<SymbolAuctionInfoIndex> s) => s.Descending(a => a.StartTime);


        var auctionInfos = await _symbolAuctionInfoIndexRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: 1);

        if (auctionInfos != null && !auctionInfos.Item2.IsNullOrEmpty())
        {
            return ObjectMapper.Map<List<SymbolAuctionInfoIndex>, List<AuctionInfoDto>>(auctionInfos.Item2);
        }
        return null;
    }

    public async Task<AuctionInfoDto> GetSymbolAuctionInfoAsync(string symbol)
    {
        if (string.IsNullOrEmpty(symbol))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Symbol).Value(symbol.ToUpper())));

        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<SymbolAuctionInfoIndex> s) => s.Descending(a => a.StartTime);

        var auctionInfos = await _symbolAuctionInfoIndexRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: 1);

        var auctionInfoDto = auctionInfos != null && !auctionInfos.Item2.IsNullOrEmpty() ? 
            ObjectMapper.Map<SymbolAuctionInfoIndex, AuctionInfoDto>(auctionInfos.Item2.First()) : null;

        if (auctionInfoDto?.StartPrice == null)
        {
            return auctionInfoDto;
        }

        var amount= FTHelper.GetRealELFAmount(auctionInfoDto.StartPrice.Amount);
        var usdPrice =
            await _tokenAppService.GetCurrentDollarPriceAsync(auctionInfoDto.StartPrice.Symbol, amount);
        auctionInfoDto.StartUsdPrice = new TokenPriceDto
        {
            Amount = usdPrice,
            Symbol = SeedConstants.UsdSymbol
        };
        
        if (auctionInfoDto.FinishPrice != null)
        {
            var realElfAmount = FTHelper.GetRealELFAmount(auctionInfoDto.FinishPrice.Amount);
            var currentDollarPriceAsync = await _tokenAppService.GetCurrentDollarPriceAsync(auctionInfoDto.FinishPrice.Symbol, realElfAmount);
            auctionInfoDto.CurrentELFPrice = auctionInfoDto.FinishPrice.Amount;
            auctionInfoDto.CurrentUSDPrice = currentDollarPriceAsync;
            await convertMarkupPrice(auctionInfoDto, auctionInfoDto.FinishPrice);

        }
        else
        {
            auctionInfoDto.CurrentELFPrice = auctionInfoDto.StartPrice.Amount;
            auctionInfoDto.CurrentUSDPrice = usdPrice;
            await convertMarkupPrice(auctionInfoDto, auctionInfoDto.StartPrice);

        }

        return auctionInfoDto;
    }

    
    public async Task convertMarkupPrice(AuctionInfoDto auctionInfoDto, TokenPriceDto tokenPriceDto)
    {
        var realElfAmount = FTHelper.GetRealELFAmount(tokenPriceDto.Amount);
        var minElfPriceMarkup = realElfAmount * (auctionInfoDto.MinMarkup / MinMarkupDenominator);
        var minDollarPriceMarkup = await _tokenAppService.GetCurrentDollarPriceAsync(tokenPriceDto.Symbol, minElfPriceMarkup);
        auctionInfoDto.MinElfPriceMarkup = minElfPriceMarkup;
        auctionInfoDto.MinDollarPriceMarkup = minDollarPriceMarkup;
        auctionInfoDto.CalculatorMinMarkup = auctionInfoDto.MinMarkup / MinMarkupDenominator;
    }
    
    public async Task<List<AuctionInfoDto>> GetUnFinishedSymbolAuctionInfoListAsync(GetAuctionInfoRequestDto input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.FinishIdentifier).Value(0)));
        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<SymbolAuctionInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.StartTime));

        var list = await _symbolAuctionInfoIndexRepository.GetSortListAsync(Filter,
            sortFunc: sorting,
            limit: input.MaxResultCount == 0 ? BidConsts.DefaultListSize : input.MaxResultCount > BidConsts.MaxListSize ? BidConsts.MaxListSize : input.MaxResultCount,
            skip: input.SkipCount);
        return !list.Item2.IsNullOrEmpty() ? ObjectMapper.Map<List<SymbolAuctionInfoIndex>, List<AuctionInfoDto>>(list.Item2) : new List<AuctionInfoDto>();
    }

    public async Task<AuctionInfoDto> GetSymbolAuctionInfoByIdAsync(string Id)
    {
        if (string.IsNullOrEmpty(Id))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(Id)));


        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var auctionInfo = await _symbolAuctionInfoIndexRepository.GetAsync(Filter);
        return ObjectMapper.Map<SymbolAuctionInfoIndex, AuctionInfoDto>(auctionInfo);
    }

    public async Task<AuctionInfoDto> GetSymbolAuctionInfoByIdAndTransactionHashAsync(string auctionId, string transactionHash)
    {
        if (string.IsNullOrEmpty(auctionId))
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolAuctionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i => i.Field(f => f.Id).Value(auctionId)));
        mustQuery.Add(q => q.Term(i => i.Field(f => f.TransactionHash).Value(transactionHash)));


        QueryContainer Filter(QueryContainerDescriptor<SymbolAuctionInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var auctionInfo = await _symbolAuctionInfoIndexRepository.GetAsync(Filter);

        return auctionInfo != null ? ObjectMapper.Map<SymbolAuctionInfoIndex, AuctionInfoDto>(auctionInfo) : null;
    }

    public async Task AddSymbolAuctionInfoListAsync(AuctionInfoDto auctionInfoDto)
    {
        var symbolAuctionInfoIndex = ObjectMapper.Map<AuctionInfoDto, SymbolAuctionInfoIndex>(auctionInfoDto);
        await _symbolAuctionInfoIndexRepository.AddOrUpdateAsync(symbolAuctionInfoIndex);
        await _bus.Publish<NewIndexEvent<AuctionInfoDto>>(new NewIndexEvent<AuctionInfoDto>
        {
            Data = auctionInfoDto
        });
    }

    public async Task UpdateSymbolAuctionInfoAsync(AuctionInfoDto auctionInfoDto)
    {
        var symbolAuctionInfoIndex = ObjectMapper.Map<AuctionInfoDto, SymbolAuctionInfoIndex>(auctionInfoDto);
        await _symbolAuctionInfoIndexRepository.AddOrUpdateAsync(symbolAuctionInfoIndex);
        
        await _bus.Publish<NewIndexEvent<AuctionInfoDto>>(new NewIndexEvent<AuctionInfoDto>
        {
            Data = auctionInfoDto
        });
    }

    public async Task AddBidInfoListAsync(BidInfoDto bidInfoDto)
    {
        var bidInfoIndex = ObjectMapper.Map<BidInfoDto, SymbolBidInfoIndex>(bidInfoDto);
        await _symbolBidInfoIndexRepository.AddOrUpdateAsync(bidInfoIndex);
        await _bus.Publish<NewIndexEvent<BidInfoDto>>(new NewIndexEvent<BidInfoDto>
        {
            Data = bidInfoDto
        });
    }

    public async Task<SeedAuctionInfoDto> GetSeedAuctionInfoAsync(string symbol)
    {
        var symbolAuctionInfo = await GetSymbolAuctionInfoAsync(symbol);
        if (symbolAuctionInfo == null)
        {
            return null;
        }

        var id = symbolAuctionInfo.Id;
        var bidInfoList = await GetBidInfoListAsync(id);
        if (bidInfoList.IsNullOrEmpty())
        {
            var seedAuctionInfo = new SeedAuctionInfoDto
            {
                TopBidPrice = new TokenPriceDto
                {
                    Symbol = symbolAuctionInfo.StartPrice.Symbol,
                    Amount = symbolAuctionInfo.StartPrice.Amount
                }
            };
            return seedAuctionInfo;
        }

        var bidderList = bidInfoList.Select(b => b.Bidder).ToList();
        var bidInfoDto = bidInfoList.First();
        var seedAuctionInfoDto = new SeedAuctionInfoDto
        {
            AuctionEndTime = symbolAuctionInfo.EndTime,
            TopBidPrice = new TokenPriceDto
            {
                Symbol = bidInfoDto.PriceSymbol,
                Amount = bidInfoDto.PriceAmount
            },
            BidderList = bidderList
        };
        return seedAuctionInfoDto;
    }

    private async Task<List<BidInfoDto>> GetBidInfoListAsync(string auctionId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SymbolBidInfoIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Term(i => i.Field(f => f.AuctionId).Value(auctionId)));
        QueryContainer Filter(QueryContainerDescriptor<SymbolBidInfoIndex> f) => f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<SymbolBidInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.BidTime));

        var list = await _symbolBidInfoIndexRepository.GetSortListAsync(Filter,
            sortFunc: sorting);

        var bidInfoList = ObjectMapper.Map<List<SymbolBidInfoIndex>, List<BidInfoDto>>(list.Item2);

        return bidInfoList;
    }
}