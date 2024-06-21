using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.Message;
using NFTMarketServer.Message.Provider;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Users.Provider;

public class UserBalanceProvider : IUserBalanceProvider, ISingletonDependency
{
    private readonly INESTRepository<UserBalanceIndex, string> _userBalanceIndexRepository;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IBus _bus;
    private readonly ILogger<MessageInfoProvider> _logger;

    public UserBalanceProvider(
        INESTRepository<UserBalanceIndex, string> userBalanceIndexRepository,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IBus bus,
        ILogger<MessageInfoProvider> logger
        )
    {
        _userBalanceIndexRepository = userBalanceIndexRepository;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _bus = bus;
        _logger = logger;
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
    

    public async Task SaveOrUpdateUserBalanceAsync(UserBalanceDto userBalanceDto)
    {
        if (userBalanceDto == null)
        {
            _logger.LogError("SaveOrUpdateUserBalanceAsync messageInfo is null");
            return;
        }

        var userBalanceList = new List<UserBalanceIndex>();
        if (userBalanceList.IsNullOrEmpty())
        {
            _logger.LogError("SaveOrUpdateUserBalanceAsync userBalanceList is null userBalanceDto={A}",
                JsonConvert.SerializeObject(userBalanceDto));
            return;
        }

        await _userBalanceIndexRepository.BulkAddOrUpdateAsync(userBalanceList);

        foreach (var userBalance in userBalanceList)
        {
            _logger.LogInformation("SaveOrUpdateUserBalanceAsync userBalance={A}",
                JsonConvert.SerializeObject(userBalance));
            if (TimeHelper.IsWithin30MinutesUtc(userBalance.ChangeTime))
            {
                await _bus.Publish(new NewIndexEvent<UserBalanceDto>
                {
                    Data = new UserBalanceDto
                    {
                        Address = userBalance.Address
                    }
                });
            }
        }
    }

    public async Task BatchSaveOrUpdateUserBalanceAsync(List<UserBalanceIndex> userBalanceIndices)
    {
        await _userBalanceIndexRepository.BulkAddOrUpdateAsync(userBalanceIndices);
    }

    /*private async Task<List<MessageInfoIndex>> BuildMessageInfoIndexListAsync(NFTMessageActivityDto activityDto)
    {
        var resultList = new List<MessageInfoIndex>();
        if (activityDto == null)
        {
            return resultList;
        }

        var fromAddress = FullAddressHelper.ToShortAddress(activityDto.From);
        var fromId = IdGenerateHelper.GetMessageActivityId(activityDto.Id, fromAddress);
        var fromMessageExist = await _messageInfoIndexRepository.GetAsync(fromId);
        if (fromMessageExist != null)
        {
            return resultList;
        }
        
        var toAddress = FullAddressHelper.ToShortAddress(activityDto.To);
        var toId = IdGenerateHelper.GetMessageActivityId(activityDto.Id, toAddress);
        var toMessageExist = await _messageInfoIndexRepository.GetAsync(toId);
        if (toMessageExist != null)
        {
            return resultList;
        }

        var symbolName = "";
        var collectionName = "";
        var decimals = 0;
        var image = "";
        if (SymbolHelper.CheckSymbolIsCommonNFTInfoId(activityDto.NFTInfoId))
        {
            var nftInfoNewIndex = await _nftInfoNewIndexRepository.GetAsync(activityDto.NFTInfoId);
            if (nftInfoNewIndex == null)
            {
                return resultList;
            }
            symbolName = nftInfoNewIndex.TokenName;
            collectionName = nftInfoNewIndex.CollectionName;
            decimals = nftInfoNewIndex.Decimals;

            image = SymbolHelper.BuildNFTImage(nftInfoNewIndex);
        }
        else
        {
            var seedInfoIndex = await _seedSymbolIndexRepository.GetAsync(activityDto.NFTInfoId);
            if (seedInfoIndex == null)
            {
                return resultList;
            }
            symbolName = seedInfoIndex.TokenName;
            collectionName = CommonConstant.CollectionSeedName;
            decimals = seedInfoIndex.Decimals;
            image = seedInfoIndex.SeedImage;
        }
        
        switch (activityDto.Type)
        {
            case NFTActivityType.Sale:
                resultList.Add(BuildSellMessageInfoIndex(fromId, fromAddress, symbolName,
                    collectionName, image, decimals, activityDto));
                resultList.Add(BuildBuyMessageInfoIndex(toId, toAddress, symbolName,
                    collectionName, image, decimals, activityDto));
                break;
            case NFTActivityType.MakeOffer:
                resultList.Add(BuildReceiveOfferMessageInfoIndex(toId, toAddress, symbolName,
                    collectionName, image, decimals, activityDto));
                break;
            default:
                break;
        }
        return resultList;
    }*/
    
}