using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using NFTMarketServer.NFT.Provider;
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

    public StatisticsAppService(
        IUserAppService userAppService,
        INFTActivityProvider nftActivityProvider,
        IObjectMapper objectMapper,
        ILogger<StatisticsAppService> logger)
    {
        _userAppService = userAppService;
        _nftActivityProvider = nftActivityProvider;
        _objectMapper = objectMapper;
        _logger = logger;
    }

    public async Task<long> GetListAsync(GetNewUserInput input)
    {
        _logger.LogInformation("StatisticsAppService.GetListAsync start min:{min}, max:{max}", input.TimestampMin, input.TimestampMax);

        var initStartTime = 1640966400;
        var types = new List<int>()
        {
            EnumHelper.GetIndex(NFTActivityType.Sale),
            EnumHelper.GetIndex(NFTActivityType.MakeOffer),
            EnumHelper.GetIndex(NFTActivityType.ListWithFixedPrice),
            EnumHelper.GetIndex(NFTActivityType.PlaceBid),
        };
        
        //get activities by time
        var currentActivityTuple = await _nftActivityProvider.GetActivityListAsync(new List<string>(), types, input.TimestampMin, input.TimestampMax);
        if (currentActivityTuple == null || currentActivityTuple.Item1 == 0)
        {
            return 0;
        }

        var currentAddresses = currentActivityTuple.Item2.Select(x => x.From).Distinct().ToList();
        _logger.LogInformation("StatisticsAppService.GetListAsync  , currentAddresses: {currentAddresses} count:{count}, min:{min}, max:{max}", currentAddresses,currentAddresses.Count, input.TimestampMin, input.TimestampMax);

        var lastDayTime = input.TimestampMax - 3600 * 24;
        var activityHistoryTuples = await _nftActivityProvider.GetActivityListAsync(currentAddresses, types, initStartTime, lastDayTime);
        if (activityHistoryTuples == null || activityHistoryTuples.Item1 == 0)
        {
            return currentAddresses.Count;
        }
        var historyAddresses = activityHistoryTuples.Item2.Select(x => x.From).Distinct().ToList();
        _logger.LogInformation("StatisticsAppService.GetListAsync  , historyAddresses: {historyAddresses} count:{count}, min:{min}, max:{max}", historyAddresses,historyAddresses.Count,input.TimestampMin, input.TimestampMax);
        var newUserCount = historyAddresses.IsNullOrEmpty()
            ? currentAddresses.Count
            : (currentAddresses.Count - historyAddresses.Count);
        return newUserCount;
    }
}