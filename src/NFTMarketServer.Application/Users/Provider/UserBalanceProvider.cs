using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;
using NFTMarketServer.Message.Provider;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Users.Provider;

public class UserBalanceProvider : IUserBalanceProvider, ISingletonDependency
{
    private readonly INESTRepository<UserBalanceIndex, string> _userBalanceIndexRepository;
    private readonly IBus _bus;
    private readonly ILogger<MessageInfoProvider> _logger;
    private readonly INFTInfoNewSyncedProvider _nftInfoNewSyncedProvider;
    private const int MaxQueryBalanceCount = 10;


    public UserBalanceProvider(
        INESTRepository<UserBalanceIndex, string> userBalanceIndexRepository,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IBus bus,
        ILogger<MessageInfoProvider> logger,
        INFTInfoNewSyncedProvider nftInfoNewSyncedProvider
        )
    {
        _userBalanceIndexRepository = userBalanceIndexRepository;
        _bus = bus;
        _logger = logger;
        _nftInfoNewSyncedProvider = nftInfoNewSyncedProvider;

    }
    public async Task<Tuple<long, List<UserBalanceIndex>>> GetUserBalancesAsync(string address, QueryUserBalanceListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        if (!address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(address)));
        }
        
        
        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<UserBalanceIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.ChangeTime));
        var tuple = await _userBalanceIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: sorting);
        return tuple;
    }
    

    public async Task SaveOrUpdateUserBalanceAsync(UserBalanceDto dto)
    {
        if (dto == null)
        {
            _logger.LogError("SaveOrUpdateUserBalanceAsync messageInfo is null");
            return;
        }
        _logger.LogInformation("SaveOrUpdateUserBalanceAsync messageInfo id:{A}",dto.Id);

        
        var nftInfoIndex = await _nftInfoNewSyncedProvider.GetNFTInfoIndexAsync(dto.NFTInfoId);
        var collectionId = "";
        var collectionSymbol = "";
        var collectionName = "";

        var nFTName = "";
        
        var decimals = 0;

        if (nftInfoIndex != null)
        {
            collectionId = nftInfoIndex.CollectionId;
            collectionSymbol = nftInfoIndex.CollectionSymbol;
            collectionName = nftInfoIndex.CollectionName;
            nFTName = nftInfoIndex.TokenName;
            decimals = nftInfoIndex.Decimals;
        }
        var FullAddress = FullAddressHelper.ToFullAddress(dto.Address, dto.ChainId);
        var userBalanceIndex = new UserBalanceIndex()
        {
            Id = dto.Id,
            Address = dto.Address,
            Amount = dto.Amount,
            NFTInfoId = dto.NFTInfoId,
            Symbol = dto.Symbol,
            ChangeTime = dto.ChangeTime,
            ListingPrice = dto.ListingPrice,
            ListingTime = dto.ListingTime,
            BlockHeight = dto.BlockHeight,
            ChainId = dto.ChainId,
            FullAddress = FullAddress,
            CollectionId = collectionId,
            Decimals = decimals,
            CollectionSymbol = collectionSymbol,
            NFTName = nFTName,
            CollectionName = collectionName
        };

        await _userBalanceIndexRepository.AddOrUpdateAsync(userBalanceIndex);
        
    }

    public async Task BatchSaveOrUpdateUserBalanceAsync(List<UserBalanceIndex> userBalanceIndices)
    {
        await _userBalanceIndexRepository.BulkAddOrUpdateAsync(userBalanceIndices);
    }

    public async Task<Tuple<long, List<UserBalanceIndex>>> GetUserBalancesAsync(QueryUserBalanceIndexInput input)
    {
        _logger.LogInformation("GetUserBalancesAsync debug query input:{A}",JsonConvert.SerializeObject(input));
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        if (!input.Address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(input.Address)));
        }

        if (input.QueryType == QueryType.HOLDING)
        {
            mustQuery.Add(q => q.TermRange(i => i.Field(index => index.Amount).GreaterThanOrEquals(1.ToString())));
        } 
        if (!input.CollectionIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.CollectionId).Terms(input.CollectionIdList)));
        }
        if (!input.KeyWord.IsNullOrEmpty())
        {        
            var shouldQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
            shouldQuery.Add(q => q.Wildcard(i => i.Field(f => f.CollectionName).Value("*" + input.KeyWord + "*")));
            shouldQuery.Add(q => q.Wildcard(i => i.Field(f => f.CollectionSymbol).Value("*" + input.KeyWord + "*")));
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<UserBalanceIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.ChangeTime));
        var tuple = await _userBalanceIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            sortFunc: sorting).ConfigureAwait(false);
        return tuple;
    }
    
    public async Task<List<UserBalanceIndex>> GetValidUserBalanceInfosAsync(QueryUserBalanceIndexInput queryUserBalanceIndexInput)
    {
        var userBalanceList = new List<UserBalanceIndex>();
        var totalCount = 0;
        var queryCount = 1;
        while (queryCount <= MaxQueryBalanceCount)
        {
            var result = await GetUserBalancesAsync(queryUserBalanceIndexInput);
            if (result == null || result.Item1 <= CommonConstant.IntZero)
            {
                return userBalanceList;
            }
            // _logger.LogDebug("GetValidUserBalanceInfosAsync for debug query userBalance count:{A} size:{size} input:{C}", result.Item1, result.Item2.Count, JsonConvert.SerializeObject(queryUserBalanceIndexInput));

            if (queryCount == CommonConstant.IntOne)
            {
                totalCount = (int)result.Item1;
            }
            userBalanceList.AddRange(result.Item2);
            // _logger.LogDebug("GetValidUserBalanceInfosAsync for debug query userBalanceList count:{A}", userBalanceList.Count);

            queryUserBalanceIndexInput.SkipCount = result.Item2.Count;
            queryCount++;
            if (totalCount == userBalanceList.Count)
            {
                break;
            }
        }
        var returnUserBalances = new List<UserBalanceIndex>();
        foreach (var userBalance in userBalanceList)
        {
            var amount = userBalance.Amount;
            var decimals = userBalance.Decimals;
            var minQuantity = (int)(1 * Math.Pow(10, decimals));
            if (amount >= minQuantity)
            {
                returnUserBalances.Add(userBalance);
            }
        }
        _logger.LogDebug("GetValidUserBalanceInfosAsync for debug query returnUserBalances count:{A}", returnUserBalances.Count);

        return returnUserBalances;
    }

    public async Task<Tuple<long, List<UserBalanceIndex>>> GetUserBalancesByNFTInfoIdAsync(QueryUserBalanceIndexInput input)
    {
        input.SkipCount = 0;
        input.MaxResultCount = 1;
        _logger.LogInformation("GetUserBalancesByNFTInfoIdAsync debug query input:{A}",JsonConvert.SerializeObject(input));
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        if (!input.Address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(input.Address)));
        }
        if (!input.NFTInfoId.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.NFTInfoId).Value(input.NFTInfoId)));
        }

        if (input.QueryType == QueryType.HOLDING)
        {
            mustQuery.Add(q => q.TermRange(i => i.Field(index => index.Amount).GreaterThanOrEquals(1.ToString())));
        } 
        if (!input.CollectionIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.CollectionId).Terms(input.CollectionIdList)));
        }
        if (!input.KeyWord.IsNullOrEmpty())
        {        
            var shouldQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
            shouldQuery.Add(q => q.Wildcard(i => i.Field(f => f.CollectionName).Value("*" + input.KeyWord + "*")));
            shouldQuery.Add(q => q.Wildcard(i => i.Field(f => f.CollectionSymbol).Value("*" + input.KeyWord + "*")));
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<UserBalanceIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.ChangeTime));
        var tuple =  await _userBalanceIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            sortFunc: sorting);
        return tuple;
    }
}