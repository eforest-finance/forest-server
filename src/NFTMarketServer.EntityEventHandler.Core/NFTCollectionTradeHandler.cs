using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.HandleException;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;

namespace NFTMarketServer.EntityEventHandler.Core;

public class NFTCollectionTradeHandler : IDistributedEventHandler<NFTCollectionTradeEto>, ITransientDependency
{
    private readonly INESTRepository<NFTCollectionExtensionIndex, string> _nftCollectionExtensionRepository;
    private readonly INESTRepository<HourlyCollectionTradeRecordIndex, string> _hourlyCollectionTradeRecordRepository;
    private readonly ILogger<NFTCollectionTradeHandler> _logger;
    private readonly INFTCollectionProvider _collectionProvider;
    private readonly INFTInfoNewSyncedProvider _nftInfoNewSyncedProvider;
    private readonly ISeedSymbolSyncedProvider _seedSymbolSyncedProvider;
    private readonly IOptionsMonitor<CollectionTradeInfoOptions> _collectionTradeInfoOptions;
    private readonly IElasticClient _elasticClient;

    public NFTCollectionTradeHandler(
        INESTRepository<NFTCollectionExtensionIndex, string> nftCollectionExtensionRepository,
        INESTRepository<HourlyCollectionTradeRecordIndex, string> hourlyCollectionTradeRecordRepository,
        INFTCollectionProvider collectionProvider,
        INFTInfoNewSyncedProvider nftInfoNewSyncedProvider,
        ISeedSymbolSyncedProvider seedSymbolSyncedProvider,
        IOptionsMonitor<CollectionTradeInfoOptions> collectionTradeInfoOptions,
        IElasticClient elasticClient,
        ILogger<NFTCollectionTradeHandler> logger)
    {
        _nftCollectionExtensionRepository = nftCollectionExtensionRepository;
        _hourlyCollectionTradeRecordRepository = hourlyCollectionTradeRecordRepository;
        _collectionProvider = collectionProvider;
        _nftInfoNewSyncedProvider = nftInfoNewSyncedProvider;
        _seedSymbolSyncedProvider = seedSymbolSyncedProvider;
        _collectionTradeInfoOptions = collectionTradeInfoOptions;
        _logger = logger;
        _elasticClient = elasticClient;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "NFTCollectionTradeHandler.HandleEventInnerAsync",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionFailRetrun),
        LogTargets = new[] { "eventData" }
    )]
    public virtual async Task<long> HandleEventInnerAsync(NFTCollectionTradeEto eventData)
    {
        _logger.LogInformation("NFTCollectionTradeEto begin, eventData={A} ",
            JsonConvert.SerializeObject(eventData));
        var collectionId = eventData?.CollectionId;
        if (collectionId.IsNullOrEmpty())
        {
            _logger.LogError("NFTCollectionTradeEto param is null. collectionId ={A} eventData={B}", collectionId,
                JsonConvert.SerializeObject(eventData));
            return 1;
        }

        var collectionTradeInfoOptions = _collectionTradeInfoOptions?.CurrentValue;
        if (collectionTradeInfoOptions != null && collectionTradeInfoOptions.GrayIsOn)
        {
            if (!collectionTradeInfoOptions.CollectionIdList.Contains(collectionId))
            {
                _logger.LogDebug("NFTCollectionTradeEto Gray mode. not contain this   collectionId={A}",
                    collectionId);
                return 1;
            }
        }

        var chainId = eventData.ChainId;
        var id = eventData.Id;
        var currentOrdinal = eventData.CurrentOrdinal;
        var nftCollectionExtensionIndex = await _nftCollectionExtensionRepository.GetAsync(collectionId);

        if (nftCollectionExtensionIndex == null)
        {
            _logger.LogError("collectionExtension is null . collectionId ={A}", collectionId);
            return 1;
        }

        var changeFlag = false;
        var temChangeFlag = false;
        temChangeFlag =
            await SaveCurrentHourRecordAsync(id, chainId, collectionId, currentOrdinal, nftCollectionExtensionIndex);

        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);
        if (eventData.InitFlag)
        {
            _logger.LogDebug("SavePreHourRecordInitAsync {A}", collectionId);
            await SavePreHourRecordInitAsync(id, chainId, collectionId, currentOrdinal);
        }
        else
        {
            await SavePreHourRecordAsync(id, chainId, collectionId, currentOrdinal);
        }

        temChangeFlag =
            await BuildPreDayFloorPriceAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildPreWeekFloorPriceAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildPreMonthFloorPriceAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildPreAllFloorPriceAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);


        temChangeFlag =
            await BuildDayTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildCurrentWeekTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildCurrentMonthTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildCurrentAllTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildPreWeekTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        temChangeFlag =
            await BuildPreMonthTradeInfoAsync(eventData.CurrentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temChangeFlag);

        var temSupplyTotal = 0l;
        if (nftCollectionExtensionIndex.NFTSymbol.Equals(SymbolHelper.SEED_COLLECTION))
        {
            temSupplyTotal = await _seedSymbolSyncedProvider.CalCollectionItemSupplyTotalAsync(chainId);
        }
        else
        {
            temSupplyTotal =
                await _nftInfoNewSyncedProvider.CalCollectionItemSupplyTotalAsync(chainId, collectionId);
        }

        if (nftCollectionExtensionIndex.SupplyTotal != temSupplyTotal)
        {
            nftCollectionExtensionIndex.SupplyTotal = temSupplyTotal;
            changeFlag = UpdateChangeFlag(changeFlag, true);
        }

        if (changeFlag)
        {
            await _nftCollectionExtensionRepository.UpdateAsync(nftCollectionExtensionIndex);
        }

        return 1;
    }

    public virtual async Task HandleEventAsync(NFTCollectionTradeEto eventData)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await HandleEventInnerAsync(eventData);
        if (result <= 0)
        {
            _logger.LogError("nftCollection trade information add or update fail: {Data}",
                JsonConvert.SerializeObject(eventData));
        }

        stopwatch.Stop();
        var elapsedMilliseconds = stopwatch.ElapsedMilliseconds;
        _logger.LogInformation("NFTCollectionTradeEto ={A} end , spend: {B} Milliseconds",
            JsonConvert.SerializeObject(eventData), elapsedMilliseconds);
    }

    private bool UpdateChangeFlag(bool originFlag, bool newTemFlag)
    {
        if (newTemFlag)
        {
            return newTemFlag;
        }

        return originFlag;
    }

    private async Task<bool> SaveCurrentHourRecordAsync(string id, string chainId, string collectionId,
        long currentOrdinal, NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginUtcStamp = currentOrdinal;
        var endUtcStamp = TimeHelper.GetNextUtcHourStartTimestamp(beginUtcStamp);
        var result =
            await SaveHourlyCollectionTradeRecordIndexAsync(beginUtcStamp, endUtcStamp, chainId, collectionId, id);

        await _hourlyCollectionTradeRecordRepository.AddOrUpdateAsync(result);

        if (nftCollectionExtensionIndex.FloorPrice != result.FloorPrice ||
            nftCollectionExtensionIndex.CurrentDaySalesTotal != result.SalesTotal ||
            nftCollectionExtensionIndex.CurrentDayVolumeTotal != result.VolumeTotal
           )
        {
            nftCollectionExtensionIndex.FloorPrice = result.FloorPrice;
            nftCollectionExtensionIndex.CurrentDaySalesTotal = result.SalesTotal;
            nftCollectionExtensionIndex.CurrentDayVolumeTotal = result.VolumeTotal;
            return true;
        }

        return false;
    }

    private async Task SavePreHourRecordAsync(string id, string chainId, string collectionId, long currentOrdinal)
    {
        var preHourTimestamp = currentOrdinal;
        for (var i = 1; i <= 24 * 14; i++)
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
            var record = await SaveHourlyCollectionTradeRecordIndexAsync(beginUtcStamp, endUtcStamp, chainId,
                collectionId,
                temId);
            await _hourlyCollectionTradeRecordRepository.AddAsync(record);
        }
    }

    [ExceptionHandler(typeof(Exception),
        Message = "NFTCollectionTradeHandler.SavePreHourRecordInitAsync error",
        TargetType = typeof(ExceptionHandlingService),
        LogOnly = true,
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new[] { "id", "chainId", "collectionId", "currentOrdinal" }
    )]
    public virtual async Task SavePreHourRecordInitAsync(string id, string chainId, string collectionId,
        long currentOrdinal)
    {
        var preHourTimestamp = currentOrdinal;
        for (var i = 1; i <= 24 * 30 * 15; i++)
        {
            _logger.LogDebug("SavePreHourRecordInitAsync {A} {B}", collectionId, i);
            preHourTimestamp = TimeHelper.GetBeforeUtcHourStartTimestamp(preHourTimestamp, 1);
            var temId = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId,
                TimeHelper.GetUnixTimestampSecondsFormatted(preHourTimestamp));
            var preRecord = await _hourlyCollectionTradeRecordRepository.GetAsync(temId);
            if (preRecord != null)
            {
                continue;
            }

            var beginUtcStamp = preHourTimestamp;
            var endUtcStamp = TimeHelper.GetNextUtcHourStartTimestamp(beginUtcStamp);
            var record = await SaveHourlyCollectionTradeRecordIndexAsync(beginUtcStamp, endUtcStamp, chainId,
                collectionId,
                temId);
            await _hourlyCollectionTradeRecordRepository.AddAsync(record);
        }
    }

    private async Task<bool> BuildDayTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var changeFlag = false;
        var temFlag = await BuildCurrentDayTradeInfoAsync(currentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temFlag);

        temFlag = await BuildPreDayTradeInfoAsync(currentOrdinal, collectionId, nftCollectionExtensionIndex);
        changeFlag = UpdateChangeFlag(changeFlag, temFlag);

        var temVolumeTotalChange = PercentageCalculatorHelper.CalculatePercentage(
            nftCollectionExtensionIndex.CurrentDayVolumeTotal, nftCollectionExtensionIndex.PreviousDayVolumeTotal);
        if (nftCollectionExtensionIndex.CurrentDayVolumeTotalChange != temVolumeTotalChange)
        {
            nftCollectionExtensionIndex.CurrentDayVolumeTotalChange = temVolumeTotalChange;
            changeFlag = UpdateChangeFlag(changeFlag, true);
        }

        return changeFlag;
    }

    private async Task<bool> BuildCurrentDayTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 23);
        var endordinal = currentOrdinal;
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);

        var changeFlag = false;
        var temVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        if (nftCollectionExtensionIndex.CurrentDayVolumeTotal != temVolumeTotal)
        {
            nftCollectionExtensionIndex.CurrentDayVolumeTotal = temVolumeTotal;
            changeFlag = true;
        }

        var temSalesTotal = resultList.Sum(obj => obj.SalesTotal);
        if (nftCollectionExtensionIndex.CurrentDaySalesTotal != temSalesTotal)
        {
            nftCollectionExtensionIndex.CurrentDaySalesTotal = temSalesTotal;
            changeFlag = true;
        }

        return changeFlag;
    }

    private async Task<bool> BuildPreDayTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 + 23);
        var endordinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24);
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);

        var changeFlag = false;

        var temVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        if (nftCollectionExtensionIndex.PreviousDayVolumeTotal != temVolumeTotal)
        {
            nftCollectionExtensionIndex.PreviousDayVolumeTotal = temVolumeTotal;
            changeFlag = true;
        }

        var temSalesTotal = resultList.Sum(obj => obj.SalesTotal);
        if (nftCollectionExtensionIndex.PreviousDaySalesTotal != temSalesTotal)
        {
            nftCollectionExtensionIndex.PreviousDaySalesTotal = temSalesTotal;
            changeFlag = true;
        }

        return changeFlag;
    }

    private async Task<bool> BuildCurrentWeekTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 6 + 23);
        var endordinal = currentOrdinal;
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);

        var changeFlag = false;
        var temVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        if (nftCollectionExtensionIndex.CurrentWeekVolumeTotal != temVolumeTotal)
        {
            nftCollectionExtensionIndex.CurrentWeekVolumeTotal = temVolumeTotal;
            changeFlag = true;
        }

        var temSalesTotal = resultList.Sum(obj => obj.SalesTotal);
        if (nftCollectionExtensionIndex.CurrentWeekSalesTotal != temSalesTotal)
        {
            nftCollectionExtensionIndex.CurrentWeekSalesTotal = temSalesTotal;
            changeFlag = true;
        }

        return changeFlag;
    }

    private async Task<bool> BuildCurrentMonthTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 29 + 23);
        var endordinal = currentOrdinal;

        var changeFlag = false;
        var temVolumeTotal = await QueryVolumeTotalSumAsync(beginOrdinal, endordinal, collectionId);
        if (nftCollectionExtensionIndex.CurrentMonthVolumeTotal != temVolumeTotal)
        {
            nftCollectionExtensionIndex.CurrentMonthVolumeTotal = temVolumeTotal;
            changeFlag = true;
        }

        var temSalesTotal = await QuerySalesTotalSumAsync(beginOrdinal, endordinal, collectionId);
        if (nftCollectionExtensionIndex.CurrentMonthSalesTotal != temSalesTotal)
        {
            nftCollectionExtensionIndex.CurrentMonthSalesTotal = temSalesTotal;
            changeFlag = true;
        }

        return changeFlag;
    }

    private async Task<bool> BuildCurrentAllTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = 0;
        var endordinal = currentOrdinal;

        var changeFlag = false;
        var temVolumeTotal = await QueryVolumeTotalSumAsync(beginOrdinal, endordinal, collectionId);
        if (nftCollectionExtensionIndex.CurrentAllVolumeTotal != temVolumeTotal)
        {
            nftCollectionExtensionIndex.CurrentAllVolumeTotal = temVolumeTotal;
            changeFlag = true;
        }

        var temSalesTotal = await QuerySalesTotalSumAsync(beginOrdinal, endordinal, collectionId);
        if (nftCollectionExtensionIndex.CurrentAllSalesTotal != temSalesTotal)
        {
            nftCollectionExtensionIndex.CurrentAllSalesTotal = temSalesTotal;
            changeFlag = true;
        }

        return changeFlag;
    }

    private async Task<bool> BuildPreWeekTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 7 * 2);
        var endordinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 7);
        var resultList = await QueryRecordList(beginOrdinal, endordinal, collectionId);

        var changeFlag = false;

        var temVolumeTotal = resultList.Sum(obj => obj.VolumeTotal);
        if (nftCollectionExtensionIndex.PreviousWeekVolumeTotal != temVolumeTotal)
        {
            nftCollectionExtensionIndex.PreviousWeekVolumeTotal = temVolumeTotal;
            changeFlag = true;
        }

        var temSalesTotal = resultList.Sum(obj => obj.SalesTotal);
        if (nftCollectionExtensionIndex.PreviousWeekSalesTotal != temSalesTotal)
        {
            nftCollectionExtensionIndex.PreviousWeekSalesTotal = temSalesTotal;
            changeFlag = true;
        }

        var temVolumeTotalChange = PercentageCalculatorHelper.CalculatePercentage(
            nftCollectionExtensionIndex.CurrentWeekVolumeTotal,
            nftCollectionExtensionIndex.PreviousWeekVolumeTotal);
        if (nftCollectionExtensionIndex.CurrentWeekVolumeTotalChange != temVolumeTotalChange)
        {
            nftCollectionExtensionIndex.CurrentWeekVolumeTotalChange = temVolumeTotalChange;
            changeFlag = true;
        }

        return changeFlag;
    }

    private async Task<bool> BuildPreMonthTradeInfoAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var beginOrdinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 30 * 2);
        var endordinal = TimeHelper.GetBeforeUtcHourStartTimestamp(currentOrdinal, 24 * 30);

        var changeFlag = false;

        var temVolumeTotal = await QueryVolumeTotalSumAsync(beginOrdinal, endordinal, collectionId);
        if (nftCollectionExtensionIndex.PreviousMonthVolumeTotal != temVolumeTotal)
        {
            nftCollectionExtensionIndex.PreviousMonthVolumeTotal = temVolumeTotal;
            changeFlag = true;
        }

        var temSalesTotal = await QuerySalesTotalSumAsync(beginOrdinal, endordinal, collectionId);
        if (nftCollectionExtensionIndex.PreviousMonthSalesTotal != temSalesTotal)
        {
            nftCollectionExtensionIndex.PreviousMonthSalesTotal = temSalesTotal;
            changeFlag = true;
        }

        var temVolumeTotalChange = PercentageCalculatorHelper.CalculatePercentage(
            nftCollectionExtensionIndex.CurrentMonthVolumeTotal,
            nftCollectionExtensionIndex.PreviousMonthVolumeTotal);
        if (nftCollectionExtensionIndex.CurrentMonthVolumeTotalChange != temVolumeTotalChange)
        {
            nftCollectionExtensionIndex.CurrentMonthVolumeTotalChange = temVolumeTotalChange;
            changeFlag = true;
        }

        return changeFlag;
    }

    public async Task<decimal> QueryVolumeTotalSumAsync(long beginOrdinal, long endOrdinal, string collectionId)
    {
        var response = await _elasticClient.SearchAsync<dynamic>(s => s
            .Index("nftmarketserver.hourlycollectiontraderecordindex")
            .Size(0)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        mu => mu.Range(dr => dr
                            .Field("ordinal")
                            .GreaterThanOrEquals(beginOrdinal)
                            .LessThanOrEquals(endOrdinal)
                        ),
                        mu => mu.Term(t => t
                            .Field("collectionId")
                            .Value(collectionId)
                        )
                    )
                )
            )
            .Aggregations(a => a
                .Sum("volume_total_sum", c => c
                    .Field("volumeTotal")
                )
            )
        );

        var volumeTotalSum = response.Aggregations.Sum("volume_total_sum").Value;
        return (decimal)volumeTotalSum.GetValueOrDefault(0);
    }

    public async Task<long> QuerySalesTotalSumAsync(long beginOrdinal, long endOrdinal, string collectionId)
    {
        var response = await _elasticClient.SearchAsync<dynamic>(s => s
            .Index("nftmarketserver.hourlycollectiontraderecordindex")
            .Size(0)
            .Query(q => q
                .Bool(b => b
                    .Must(
                        mu => mu.Range(dr => dr
                            .Field("ordinal")
                            .GreaterThanOrEquals(beginOrdinal)
                            .LessThanOrEquals(endOrdinal)
                        ),
                        mu => mu.Term(t => t
                            .Field("collectionId")
                            .Value(collectionId)
                        )
                    )
                )
            )
            .Aggregations(a => a
                .Sum("sales_total_sum", c => c
                    .Field("salesTotal")
                )
            )
        );

        var volumeTotalSum = response.Aggregations.Sum("sales_total_sum").Value;
        return (long)volumeTotalSum.GetValueOrDefault(0);
    }

    private async Task<List<HourlyCollectionTradeRecordIndex>> QueryRecordList(long beginOrdinal, long endOrdinal,
        string collectionId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<HourlyCollectionTradeRecordIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.Ordinal).GreaterThanOrEquals(beginOrdinal)));
        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.Ordinal).LessThanOrEquals(endOrdinal)));
        mustQuery.Add(q => q.Term(i
            => i.Field(f => f.CollectionId).Value(collectionId)));

        QueryContainer Filter(QueryContainerDescriptor<HourlyCollectionTradeRecordIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var result = await _hourlyCollectionTradeRecordRepository.GetSortListAsync(Filter);
        return result?.Item2;
    }

    private async Task<bool> BuildPreDayFloorPriceAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var preDayOrdinal = TimeHelper.GetPreDayUtcHourStartTimestamp(currentOrdinal);
        var preId = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId,
            TimeHelper.GetUnixTimestampSecondsFormatted(preDayOrdinal));
        var preDayRecord = await _hourlyCollectionTradeRecordRepository.GetAsync(preId);
        if (preDayRecord != null && preDayOrdinal != preDayRecord.FloorPrice)
        {
            nftCollectionExtensionIndex.PreviousDayFloorPrice = preDayRecord.FloorPrice;
            nftCollectionExtensionIndex.CurrentDayFloorChange =
                PercentageCalculatorHelper.CalculatePercentage(nftCollectionExtensionIndex.FloorPrice,
                    nftCollectionExtensionIndex.PreviousDayFloorPrice);
            return true;
        }

        return false;
    }

    private async Task<bool> BuildPreWeekFloorPriceAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var preWeekOrdinal = TimeHelper.GetPreWeekUtcHourStartTimestamp(currentOrdinal);
        var preId = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId,
            TimeHelper.GetUnixTimestampSecondsFormatted(preWeekOrdinal));
        var preWeekRecord = await _hourlyCollectionTradeRecordRepository.GetAsync(preId);
        if (preWeekRecord != null && nftCollectionExtensionIndex.PreviousWeekFloorPrice != preWeekRecord.FloorPrice)
        {
            nftCollectionExtensionIndex.PreviousWeekFloorPrice = preWeekRecord.FloorPrice;
            nftCollectionExtensionIndex.CurrentWeekFloorChange =
                PercentageCalculatorHelper.CalculatePercentage(nftCollectionExtensionIndex.FloorPrice,
                    nftCollectionExtensionIndex.PreviousWeekFloorPrice);
            return true;
        }

        return false;
    }

    private async Task<bool> BuildPreMonthFloorPriceAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        var preMonthOrdinal = TimeHelper.GetPreMonthUtcHourStartTimestamp(currentOrdinal);
        var preId = IdGenerateHelper.GetHourlyCollectionTradeRecordId(collectionId,
            TimeHelper.GetUnixTimestampSecondsFormatted(preMonthOrdinal));
        var preMonthRecord = await _hourlyCollectionTradeRecordRepository.GetAsync(preId);
        if (preMonthRecord != null && nftCollectionExtensionIndex.PreviousMonthFloorPrice != preMonthRecord.FloorPrice)
        {
            nftCollectionExtensionIndex.PreviousMonthFloorPrice = preMonthRecord.FloorPrice;
            nftCollectionExtensionIndex.CurrentMonthFloorChange = PercentageCalculatorHelper.CalculatePercentage(
                nftCollectionExtensionIndex.FloorPrice, nftCollectionExtensionIndex.PreviousMonthFloorPrice);
            return true;
        }

        return false;
    }

    private async Task<bool> BuildPreAllFloorPriceAsync(long currentOrdinal, string collectionId,
        NFTCollectionExtensionIndex nftCollectionExtensionIndex)
    {
        if (nftCollectionExtensionIndex.PreviousAllFloorPrice != null &&
            nftCollectionExtensionIndex.PreviousAllFloorPrice != nftCollectionExtensionIndex.FloorPrice)
        {
            nftCollectionExtensionIndex.PreviousAllFloorPrice = nftCollectionExtensionIndex.FloorPrice;
            nftCollectionExtensionIndex.CurrentAllFloorChange = 0;
            return true;
        }

        return false;
    }

    private async Task<HourlyCollectionTradeRecordIndex> SaveHourlyCollectionTradeRecordIndexAsync(long beginUtcStamp,
        long endUtcStamp, string chainId, string collectionId, string id)
    {
        var attributionTime = TimeHelper.FromUnixTimestampSeconds(beginUtcStamp);
        var result = await _collectionProvider.GetNFTCollectionTradeAsync(
            chainId, collectionId, beginUtcStamp, endUtcStamp);

        var hourlyCollectionTradeRecordIndex = await _hourlyCollectionTradeRecordRepository.GetAsync(id);

        if (hourlyCollectionTradeRecordIndex == null)
        {
            hourlyCollectionTradeRecordIndex = new HourlyCollectionTradeRecordIndex
            {
                Id = id,
                AttributionTime = attributionTime,
                CollectionId = collectionId,
                CreateTime = TimeHelper.GetUtcNow(),
                Ordinal = beginUtcStamp,
                BeginUtcStamp = beginUtcStamp,
                EndUtcStamp = endUtcStamp,
                OrdinalStr = TimeHelper.GetDateTimeFormatted(attributionTime),
                UpdateTime = TimeHelper.GetUtcNow(),

                FloorPrice = -1,
                VolumeTotal = 0,
                SalesTotal = 0
            };
        }

        hourlyCollectionTradeRecordIndex.FloorPrice = result.FloorPrice;
        hourlyCollectionTradeRecordIndex.VolumeTotal = result.VolumeTotal;
        hourlyCollectionTradeRecordIndex.SalesTotal = result.SalesTotal;

        return hourlyCollectionTradeRecordIndex;
    }
}