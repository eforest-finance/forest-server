using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.Message.Provider;

public class MessageInfoProvider : IMessageInfoProvider, ISingletonDependency
{
    private readonly INESTRepository<MessageInfoIndex, string> _messageInfoIndexRepository;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IDistributedEventBus _distributedEventBus;

    public MessageInfoProvider(
        INESTRepository<MessageInfoIndex, string> messageInfoIndexRepository,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IDistributedEventBus distributedEventBus
        )
    {
        _messageInfoIndexRepository = messageInfoIndexRepository;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _distributedEventBus = distributedEventBus;
    }
    public async Task<Tuple<long, List<MessageInfoIndex>>> GetUserMessageInfosAsync(string address, QueryMessageListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<MessageInfoIndex>, QueryContainer>>();
        if (!address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(address)));
        }

        if (input.Status == CommonConstant.MessageReadStatus || input.Status == CommonConstant.MessageUnReadStatus)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Status).Value(input.Status)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<MessageInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<MessageInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.Ctime));
        var tuple = await _messageInfoIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: sorting);
        return tuple;
    }

    public async Task SaveOrUpdateMessageInfoAsync(NFTMessageActivityDto nftMessageActivityDto)
    {
        if (nftMessageActivityDto == null)
        {
            return;
        }

        var messageInfoList = await BuildMessageInfoIndexListAsync(nftMessageActivityDto);
        if (messageInfoList.IsNullOrEmpty())
        {
            return;
        }

        await _messageInfoIndexRepository.BulkAddOrUpdateAsync(messageInfoList);

        foreach (var messageInfo in messageInfoList)
        {
            if(messageInfo == null || messageInfo.Address.IsNullOrEmpty()) continue;
            if (TimeHelper.IsWithin30MinutesUtc(messageInfo.Ctime))
            {
                await _distributedEventBus.PublishAsync(new MessageChangeEto
                {
                    Address = messageInfo.Address
                });
            }
        }
    }

    private async Task<List<MessageInfoIndex>> BuildMessageInfoIndexListAsync(NFTMessageActivityDto activityDto)
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
    }

    private MessageInfoIndex BuildSellMessageInfoIndex(string fromId, string fromAddress, string symbolName,
        string collectionName, string image, int decimals, NFTMessageActivityDto activityDto)
    {
        return new MessageInfoIndex()
        {
            Id = fromId,
            Address = fromAddress,
            Status = 0,
            BusinessType = BusinessType.NOTIFICATIONS,
            SecondLevelType = SecondLevelType.SELL,
            Title = symbolName,
            Body = collectionName,
            Image = image,
            Decimal = decimals,
            PriceType = activityDto.PriceTokenInfo.Symbol,
            SinglePrice = activityDto.Price,
            TotalPrice = activityDto.Price * (activityDto.Amount / (decimal)Math.Pow(10, decimals)),
            BusinessId = activityDto.NFTInfoId,
            Amount = activityDto.Amount,
            Ctime = activityDto.Timestamp,
            Utime = activityDto.Timestamp,
        };
    }
    private MessageInfoIndex BuildBuyMessageInfoIndex(string toId, string toAddress, string symbolName,
        string collectionName, string image, int decimals, NFTMessageActivityDto activityDto)
    {
        return new MessageInfoIndex()
        {
            Id =  toId,
            Address = toAddress,
            Status = 0,
            BusinessType = BusinessType.NOTIFICATIONS,
            SecondLevelType = SecondLevelType.BUY,
            Title = symbolName,
            Body = collectionName,
            Image = image,
            Decimal = decimals,
            PriceType = activityDto.PriceTokenInfo.Symbol,
            SinglePrice = activityDto.Price,
            TotalPrice = activityDto.Price * (activityDto.Amount / (decimal)Math.Pow(10, decimals)),
            BusinessId = activityDto.NFTInfoId,
            Amount = activityDto.Amount,
            Ctime = activityDto.Timestamp,
            Utime = activityDto.Timestamp,
        };
    }
    private MessageInfoIndex BuildReceiveOfferMessageInfoIndex(string toId, string toAddress, string symbolName,
        string collectionName, string image, int decimals, NFTMessageActivityDto activityDto)
    {
        return new MessageInfoIndex()
        {
            Id =  toId,
            Address = toAddress,
            Status = 0,
            BusinessType = BusinessType.NOTIFICATIONS,
            SecondLevelType = SecondLevelType.RECEIVEOFFER,
            Title = symbolName,
            Body = collectionName,
            Image = image,
            Decimal = decimals,
            PriceType = activityDto.PriceTokenInfo.Symbol,
            SinglePrice = activityDto.Price,
            TotalPrice = activityDto.Price * (activityDto.Amount / (decimal)Math.Pow(10, decimals)),
            BusinessId = activityDto.NFTInfoId,
            Amount = activityDto.Amount,
            Ctime = activityDto.Timestamp,
            Utime = activityDto.Timestamp,
        };
    }
}