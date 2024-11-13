using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.TreeGame.Provider;
using NFTMarketServer.Users;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT;

[RemoteService(IsEnabled = false)]
public class StatisticsAppService : NFTMarketServerAppService, IStatisticsAppService
{
    private readonly IUserAppService _userAppService;
    private readonly INFTActivityProvider _nftActivityProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<StatisticsAppService> _logger;
    private readonly ITreeGamePointsRecordProvider _treeGamePointsRecordProvider;

    public StatisticsAppService(
        IUserAppService userAppService,
        INFTActivityProvider nftActivityProvider,
        IObjectMapper objectMapper,
        ITreeGamePointsRecordProvider treeGamePointsRecordProvider,
        ILogger<StatisticsAppService> logger)
    {
        _userAppService = userAppService;
        _nftActivityProvider = nftActivityProvider;
        _objectMapper = objectMapper;
        _logger = logger;
        _treeGamePointsRecordProvider = treeGamePointsRecordProvider;
    }

    public async Task<long> GetListAsync(GetNewUserInput input)
    {
        _logger.LogInformation("StatisticsAppService.GetListAsync start min:{min}, max:{max}", input.TimestampMin, input.TimestampMax);

        var initStartTime = 1640966400000;
        //1.get current: nft activity
        var types = new List<int>()
        {
            EnumHelper.GetIndex(NFTActivityType.Sale),
            EnumHelper.GetIndex(NFTActivityType.MakeOffer),
            EnumHelper.GetIndex(NFTActivityType.ListWithFixedPrice),
            EnumHelper.GetIndex(NFTActivityType.PlaceBid),
        };
        var currentActivityTuple = await _nftActivityProvider.GetActivityListAsync(new List<string>(), types, input.TimestampMin, input.TimestampMax);
        var currentAddresses = new List<string>();
        if (currentActivityTuple != null && currentActivityTuple.Item1 > 0)
        {
            currentAddresses = currentActivityTuple.Item2.Select(x => x.From).Distinct().ToList();
        }
        _logger.LogInformation("StatisticsAppService.GetListAsync,nft Current: count:{count}, min:{min}, max:{max}", currentAddresses.Count, input.TimestampMin, input.TimestampMax);
        //2.get current: tree activity
        var treePointsRecord = await _treeGamePointsRecordProvider.GetTreePointsRecordsAsync(new List<string>(), input.TimestampMin, input.TimestampMax);
        var treeCurrentCount = 0;
        if (treePointsRecord != null && treePointsRecord.TotalRecordCount >= 0 &&
            !treePointsRecord.TreePointsChangeRecordList.IsNullOrEmpty())
        {
            var treeCurrentAddress = treePointsRecord.TreePointsChangeRecordList.Select(x=>x.Address).Distinct().ToList();
            currentAddresses.AddRange(treeCurrentAddress);
            treeCurrentCount = treeCurrentAddress.Count;
        }
        _logger.LogInformation("StatisticsAppService.GetListAsync,tree Current: count:{count}, min:{min}, max:{max}", treeCurrentCount, input.TimestampMin, input.TimestampMax);
        currentAddresses = currentAddresses.Distinct().ToList();
        _logger.LogInformation("StatisticsAppService.GetListAsync,total Current: count:{count}, min:{min}, max:{max}", currentAddresses.Count(), input.TimestampMin, input.TimestampMax);
        if (currentAddresses.IsNullOrEmpty())
        {
            return 0;
        }

        //3.get history : nft activity
        var lastDayTime = input.TimestampMax - 3600 * 24 * 1000;
        var activityHistoryTuples = await _nftActivityProvider.GetActivityListAsync(currentAddresses, types, initStartTime, lastDayTime);
        var historyAddresses = new List<string>();
        if (activityHistoryTuples != null && activityHistoryTuples.Item1 >= 0)
        {
            historyAddresses = activityHistoryTuples.Item2.Select(x => x.From).Distinct().ToList();
        }
        _logger.LogInformation("StatisticsAppService.GetListAsync,nft History: count:{count}, min:{min}, max:{max}", historyAddresses.Count,input.TimestampMin, input.TimestampMax);
       
        //4.get history : tree activity
        var treeHistoryRecord = await _treeGamePointsRecordProvider.GetTreePointsRecordsAsync(currentAddresses, initStartTime, lastDayTime);
        var treeHistoryCount = 0;
        if (treeHistoryRecord != null && treeHistoryRecord.TotalRecordCount >= 0 &&
            !treeHistoryRecord.TreePointsChangeRecordList.IsNullOrEmpty())
        {
            var treeHistoryAddress = treeHistoryRecord.TreePointsChangeRecordList.Select(x=>x.Address).Distinct().ToList();
            historyAddresses.AddRange(treeHistoryAddress);
            treeHistoryCount = treeHistoryAddress.Count;
        }
        _logger.LogInformation("StatisticsAppService.GetListAsync,tree History: count:{count}, min:{min}, max:{max}", treeHistoryCount, input.TimestampMin, input.TimestampMax);
        historyAddresses = historyAddresses.Distinct().ToList();
        _logger.LogInformation("StatisticsAppService.GetListAsync,total History: count:{count}, min:{min}, max:{max}", historyAddresses.Count(), input.TimestampMin, input.TimestampMax);
        
        var newUserCount = historyAddresses.IsNullOrEmpty()
            ? currentAddresses.Count
            : (currentAddresses.Count - historyAddresses.Count);
        return newUserCount;
    }
}