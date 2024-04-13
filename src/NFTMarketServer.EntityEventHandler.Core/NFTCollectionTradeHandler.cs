using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTCollectionTradeHandler : IDistributedEventHandler<NFTCollectionTradeEto>, ITransientDependency
{
    private readonly INESTRepository<NFTCollectionExtensionIndex, string> _nftCollectionExtensionRepository;
    private readonly INESTRepository<HourlyCollectionTradeRecordIndex, string> _hourlyCollectionTradeRecordRepository;
    private readonly ILogger<NFTCollectionTradeHandler> _logger;
    private readonly INFTCollectionProvider _collectionProvider;
    private readonly INFTInfoNewSyncedProvider _nftInfoNewSyncedProvider;
    private readonly ISeedSymbolSyncedProvider _seedSymbolSyncedProvider;

    public NFTCollectionTradeHandler(
        INESTRepository<NFTCollectionExtensionIndex, string> nftCollectionExtensionRepository,
        INESTRepository<HourlyCollectionTradeRecordIndex, string> hourlyCollectionTradeRecordRepository,
        INFTCollectionProvider collectionProvider,
        INFTInfoNewSyncedProvider nftInfoNewSyncedProvider,
        ISeedSymbolSyncedProvider seedSymbolSyncedProvider,
        ILogger<NFTCollectionTradeHandler> logger)
    {
        _nftCollectionExtensionRepository = nftCollectionExtensionRepository;
        _hourlyCollectionTradeRecordRepository = hourlyCollectionTradeRecordRepository;
        _collectionProvider = collectionProvider;
        _nftInfoNewSyncedProvider = nftInfoNewSyncedProvider;
        _seedSymbolSyncedProvider = seedSymbolSyncedProvider;
        _logger = logger;
    }

    public async Task HandleEventAsync(NFTCollectionTradeEto eventData)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            _logger.LogInformation("NFTCollectionTradeEto begin, eventData={A} ", JsonConvert.SerializeObject(eventData));
            
            var collectionId = eventData.CollectionId;
            var chainId = eventData.ChainId;
            var id = eventData.Id;
            var currentOrdinal = eventData.CurrentOrdinal;

            var nftCollectionExtensionIndex = await _nftCollectionExtensionRepository.GetAsync(collectionId);

            if (nftCollectionExtensionIndex == null)
            {
                _logger.LogError("collectionExtension is null . collectionId ={A}", collectionId);
                return;
            }
            
            await SaveCurrentHourRecordAsync(id, chainId, collectionId, currentOrdinal,nftCollectionExtensionIndex);
            await SavePreHourRecordAsync(id, chainId, collectionId, currentOrdinal);
            await BuildPreDayFloorPriceAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
            await BuildPreWeekFloorPriceAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
            await BuildDayTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
            await BuildCurrentWeekTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
            await BuildPreWeekTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
            nftCollectionExtensionIndex.CurrentWeekVolumeTotalChange = PercentageCalculatorHelper.CalculatePercentage(
                nftCollectionExtensionIndex.CurrentWeekVolumeTotal,
                nftCollectionExtensionIndex.PreviousWeekVolumeTotal);
            if (nftCollectionExtensionIndex.NFTSymbol.Equals(SymbolHelper.SEED_COLLECTION))
            {
                nftCollectionExtensionIndex.SupplyTotal =
                    await _seedSymbolSyncedProvider.CalCollectionItemSupplyTotalAsync(chainId);
            }
            else
            {
                nftCollectionExtensionIndex.SupplyTotal =
                    await _nftInfoNewSyncedProvider.CalCollectionItemSupplyTotalAsync(chainId, collectionId);
            }
            
            await _nftCollectionExtensionRepository.UpdateAsync(nftCollectionExtensionIndex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "nftCollection trade information add or update fail: {Data}",
                JsonConvert.SerializeObject(eventData));
        }
        finally
        {
            stopwatch.Stop();
            var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
            _logger.LogInformation("NFTCollectionTradeEto ={A} end , spend: {B} Milliseconds",
                JsonConvert.SerializeObject(eventData), elapsedMilliseconds);
        }
    }

    private async Task SaveCurrentHourRecordAsync(string id, string chainId, string collectionId, long currentOrdinal,NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginUtcStamp = currentOrdinal;
        var endUtcStamp = TimeHelper.GetNextUtcHourStartTimestamp(beginUtcStamp);
        var result = await SaveHourlyCollectionTradeRecordIndexAsync(beginUtcStamp, endUtcStamp, chainId, collectionId, id);
        nftCollectionExtensionIndex.FloorPrice = result.FloorPrice;
    }
    
    private async Task SavePreHourRecordAsync(string id, string chainId, string collectionId, long currentOrdinal)
    {
        var preHourTimestamp = currentOrdinal;
        for (var i = 1; i <= 1; i++)
        {
            preHourTimestamp = TimeHelper.GetBeforeUtcHourStartTimestamp(preHourTimestamp, 1);
            var temId = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId,
                TimeHelper.GetUnixTimestampSecondsFormatted(preHourTimestamp));
            var preRecord = await _hourlyCollectionTradeRecordRepository.GetAsync(temId);
            if (preRecord != null)
            {
                break;
            }

            var beginUtcStamp = preHourTimestamp;
            var endUtcStamp = TimeHelper.GetNextUtcHourStartTimestamp(beginUtcStamp);
            await SaveHourlyCollectionTradeRecordIndexAsync(beginUtcStamp, endUtcStamp, chainId, collectionId,
                temId);
        }
    }

    private async Task BuildDayTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        await BuildCurrentDayTradeInfoAsync(currentOrdinal, collectionId, nftCollectionExtensionIndex);
        await BuildPreDayTradeInfoAsync(currentOrdinal, collectionId, nftCollectionExtensionIndex);
        nftCollectionExtensionIndex.CurrentDayVolumeTotalChange = PercentageCalculatorHelper.CalculatePercentage(nftCollectionExtensionIndex.CurrentDayVolumeTotal,nftCollectionExtensionIndex.PreviousDayVolumeTotal);
    }
    private async Task BuildCurrentDayTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 23);
        var endordinal = currentOrdinal;
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);
        nftCollectionExtensionIndex.CurrentDayVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        nftCollectionExtensionIndex.CurrentDaySalesTotal = resultList.Sum(obj => obj.SalesTotal);
    }

    private async Task BuildPreDayTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 + 23);
        var endordinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24);
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);
        nftCollectionExtensionIndex.PreviousDayVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        nftCollectionExtensionIndex.PreviousDaySalesTotal = resultList.Sum(obj => obj.SalesTotal);
    }

    private async Task BuildCurrentWeekTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 6 + 23);
        var endordinal = currentOrdinal;
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);
        nftCollectionExtensionIndex.CurrentWeekVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        nftCollectionExtensionIndex.CurrentWeekSalesTotal = resultList.Sum(obj => obj.SalesTotal);
    }

    private async Task BuildPreWeekTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 7 * 2);
        var endordinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 7);
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);
        nftCollectionExtensionIndex.PreviousWeekVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        nftCollectionExtensionIndex.PreviousWeekSalesTotal = resultList.Sum(obj => obj.SalesTotal);
    }

    private async Task<List<HourlyCollectionTradeRecordIndex>> QueryRecordList(long beginOrdinal,long endOrdinal,string collectionId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<HourlyCollectionTradeRecordIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.Ordinal).GreaterThanOrEquals(beginOrdinal)));
        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.Ordinal).LessThanOrEquals(endOrdinal)));
        mustQuery.Add(q => q.Term(i
            => i.Field(f => f.CollectionId).Value(collectionId)));
        QueryContainer Filter(QueryContainerDescriptor<HourlyCollectionTradeRecordIndex> f)=> 
            f.Bool(b => b.Must(mustQuery));
        
        var result = await _hourlyCollectionTradeRecordRepository.GetSortListAsync(Filter);
        return result?.Item2;
    }
    
    private async Task BuildPreDayFloorPriceAsync(long currentOrdinal,string collectionId,NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var preDayOrdinal = TimeHelper.GetPreDayUtcHourStartTimestamp(currentOrdinal);
        var preId = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId,TimeHelper.GetUnixTimestampSecondsFormatted(preDayOrdinal));
        var preDayRecord = await _hourlyCollectionTradeRecordRepository.GetAsync(preId);
        if (preDayRecord != null)
        {
            nftCollectionExtensionIndex.PreviousDayFloorPrice = preDayRecord.FloorPrice;
        }
        nftCollectionExtensionIndex.CurrentDayFloorChange = PercentageCalculatorHelper.CalculatePercentage(nftCollectionExtensionIndex.FloorPrice,nftCollectionExtensionIndex.PreviousDayFloorPrice);

    }
    
    private async Task BuildPreWeekFloorPriceAsync(long currentOrdinal,string collectionId,NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var preWeekOrdinal = TimeHelper.GetPreWeekUtcHourStartTimestamp(currentOrdinal);
        var preId = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId,TimeHelper.GetUnixTimestampSecondsFormatted(preWeekOrdinal));
        var preWeekRecord = await _hourlyCollectionTradeRecordRepository.GetAsync(preId);
        if (preWeekRecord != null)
        {
            nftCollectionExtensionIndex.PreviousWeekFloorPrice = preWeekRecord.FloorPrice;
        }
        nftCollectionExtensionIndex.CurrentWeekFloorChange = PercentageCalculatorHelper.CalculatePercentage(nftCollectionExtensionIndex.FloorPrice,nftCollectionExtensionIndex.PreviousWeekFloorPrice);
    }

    private async Task<HourlyCollectionTradeRecordIndex> SaveHourlyCollectionTradeRecordIndexAsync(long beginUtcStamp,long endUtcStamp,string chainId,string collectionId,string id)
    {
        var attributionTime = TimeHelper.FromUnixTimestampSeconds(beginUtcStamp);
        var result = await _collectionProvider.GetNFTCollectionTradeAsync(
            chainId, collectionId, beginUtcStamp, endUtcStamp);
        var hourlyCollectionTradeRecordIndex = new HourlyCollectionTradeRecordIndex
        {
            Id = id,
            AttributionTime = attributionTime,
            CollectionId = collectionId,
            CreateTime = TimeHelper.GetUtcNow(),
            Ordinal = beginUtcStamp,
            OrdinalStr = TimeHelper.GetDateTimeFormatted(attributionTime),
            UpdateTime = TimeHelper.GetUtcNow(),

            FloorPrice = -1,
            VolumeTotal = 0,
            SalesTotal = 0
        };
        if (result != null)
        {
            hourlyCollectionTradeRecordIndex.FloorPrice = result.FloorPrice;
            hourlyCollectionTradeRecordIndex.VolumeTotal = result.VolumeTotal;
            hourlyCollectionTradeRecordIndex.SalesTotal = result.SalesTotal;
        }

        await _hourlyCollectionTradeRecordRepository.AddOrUpdateAsync(hourlyCollectionTradeRecordIndex);
        return hourlyCollectionTradeRecordIndex;

    }
    
}