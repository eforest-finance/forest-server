using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.Tree;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Tree;
using NFTMarketServer.Tree.Provider;
using NFTMarketServer.TreeGame;
using NFTMarketServer.TreeGame.Provider;
using NFTMarketServer.Users.Index;
using NFTMarketServer.Users.Provider;
using Orleans;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core;

public class TreePointsChangeRecordEventHandler : IDistributedEventHandler<TreePointsChangeRecordEto>, ISingletonDependency
{
    private const int ExpireSeconds = 10;
    private readonly ILogger<TreePointsChangeRecordEventHandler> _logger;
    private readonly IDistributedCache<string> _distributedCacheForHeight;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IUserBalanceProvider _userBalanceProvider;
    private readonly ITreeGameUserInfoProvider _treeGameUserInfoProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly ITreeGamePointsDetailProvider _treeGamePointsDetailProvider;
    private readonly ITreeActivityProvider _treeActivityProvider;

    private readonly IOptionsMonitor<TreeGameOptions> _platformOptionsMonitor;
    private readonly IClusterClient _clusterClient;



    public TreePointsChangeRecordEventHandler(ILogger<TreePointsChangeRecordEventHandler> logger,
        IDistributedCache<string> distributedCacheForHeight,
        INFTInfoAppService nftInfoAppService,
        ITreeGameUserInfoProvider treeGameUserInfoProvider,
        IUserBalanceProvider userBalanceProvider,
        ITreeGamePointsDetailProvider treeGamePointsDetailProvider,
        ITreeActivityProvider treeActivityProvider,
        IOptionsMonitor<TreeGameOptions> platformOptionsMonitor,
        IClusterClient clusterClient,
        IObjectMapper objectMapper)
    {
        _logger = logger;
        _distributedCacheForHeight = distributedCacheForHeight;
        _nftInfoAppService = nftInfoAppService;
        _userBalanceProvider = userBalanceProvider;
        _treeGameUserInfoProvider = treeGameUserInfoProvider;
        _objectMapper = objectMapper;
        _treeGamePointsDetailProvider = treeGamePointsDetailProvider;
        _platformOptionsMonitor = platformOptionsMonitor;
        _clusterClient = clusterClient;
        _treeActivityProvider = treeActivityProvider;
    }

    public async Task HandleEventAsync(TreePointsChangeRecordEto etoData)
    {
        _logger.LogInformation("TreePointsChangeRecordEventHandler receive: {Data}", JsonConvert.SerializeObject(etoData));
        var item = etoData.TreePointsChangeRecordItem;
        if (item == null || item.Id.IsNullOrEmpty()) return;
        var expireFlag = await _distributedCacheForHeight.GetAsync(item.Id);
        if (!expireFlag.IsNullOrEmpty())
        {
            _logger.LogInformation("TreePointsChangeRecordEventHandler expireFlag: {expireFlag},Id:{Id}",expireFlag, item.Id);
            return;
        }
        await _distributedCacheForHeight.SetAsync(item.Id,
            item.Id, new DistributedCacheEntryOptions
            {
                AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(ExpireSeconds)
            });
        
        //change points
        //change tree
        //change points detail
        var userInfo = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(item.Address);
        if (userInfo == null)
        {
            _logger.LogInformation("TreePointsChangeRecordEventHandler treeGameUserInfo is null: {A}",item.Address);
            return;
        }
        var treeInfo = await GetTreeGameTreeInfoAsync(userInfo.TreeLevel);
        if (treeInfo == null)
        {
            _logger.LogInformation("TreePointsChangeRecordEventHandler treeInfo is null: {A}",item.Address);
            return;
        }
        
        if (item.OpType == OpType.ADDED)
        {
            userInfo.Points = item.TotalPoints;
            await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(_objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(userInfo));
            var pointsDetailList = await _treeGamePointsDetailProvider.GetTreePointsDetailsAsync(item.Address);
            foreach (var detail in pointsDetailList)
            {
                if (EnumHelper.ToEnumString(detail.Type).Equals(EnumHelper.ToEnumString(item.PointsType)))
                {
                    detail.Amount = treeInfo.Current.Produce;
                    detail.UpdateTime = item.OpTime;
                    detail.RemainingTime = treeInfo.Current.Frequency;
                    await _treeGamePointsDetailProvider.SaveOrUpdateTreePointsDetailAsync(detail);
                }
            }
            //update invite parent user points
            {
                if (!userInfo.ParentAddress.IsNullOrEmpty())
                {
                    var parentPointsDetailList = await _treeGamePointsDetailProvider.GetTreePointsDetailsAsync(userInfo.ParentAddress);
                    if (parentPointsDetailList.IsNullOrEmpty())
                    {
                        _logger.LogInformation("TreePointsChangeRecordEventHandler treeGameUserInfo's parent userTreePointsDetails is null: child:{A}, parent:{B}",item.Address, userInfo.ParentAddress);
                    }
                    else
                    {
                        foreach (var detail in parentPointsDetailList)
                        {
                            if (detail.Type == PointsDetailType.INVITE)
                            {
                                var rewardProportion = TreeGameConstants.RewardProportion;
                                var rewardConfig = _platformOptionsMonitor.CurrentValue.InviteReward;
                                if (rewardConfig != null)
                                {
                                    rewardProportion = rewardConfig.RewardProportion;
                                }
                                detail.Amount += (decimal)item.PointsType * (decimal)rewardProportion;
                                await _treeGamePointsDetailProvider.SaveOrUpdateTreePointsDetailAsync(detail);
                                break;
                            }
                        }
                    }


                }
            }
        }
        
        if (item.OpType == OpType.UPDATETREE)
        {
            userInfo.Points = item.TotalPoints;
            userInfo.TreeLevel = Convert.ToInt32(item.TreeLevel);
            await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(_objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(userInfo));
            
            var pointsDetailList = await _treeGamePointsDetailProvider.GetTreePointsDetailsAsync(item.Address);
            var treeLevels = GetTreeLevelInfoConfig();
            treeInfo = new TreeInfo();
            var currentLevel = treeLevels.FirstOrDefault(x => x.Level == Convert.ToInt32(item.TreeLevel));
            if (currentLevel == null)
            {
                throw new Exception("TreePointsChangeRecordEventHandler Invalid treelevel:"+item.TreeLevel);
            }

            foreach (var detail in pointsDetailList)
            {
                if (detail.Type == PointsDetailType.INVITE)
                {
                    continue;
                }

                detail.Amount = currentLevel.Produce;
            }
            await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(userInfo.Address,pointsDetailList);

        }
        
        if (item.OpType == OpType.CLAIM)
        {
            userInfo.Points = item.TotalPoints;
            await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(_objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(userInfo));
            //update activity
            var activityId = item.ActivityId;
            var activity = await _treeActivityProvider.GetTreeActivityDetailAsync(activityId);
            var leftReward = activity.LeftReward - activity.RedeemRewardOnce;
            activity.LeftReward = leftReward;
            if (leftReward <= 0)
            {
                activity.TreeActivityStatus = TreeActivityStatus.Ended;
            }

            await _treeActivityProvider.UpdateTreeActivityDetailAsync(activity);
            
            //record user join this activity count
            var activityRecordGrain = _clusterClient.GetGrain<ITreeUserActivityRecordGrain>(string.Concat(item.Address,"_",item.ActivityId));
            var activityRecord = await activityRecordGrain.GetTreeUserActivityRecordAsync();
            await activityRecordGrain.SetTreeUserActivityRecordAsync(new TreeUserActivityRecordGrainDto()
            {
                Id = item.Id,
                ActivityId = item.ActivityId,
                Address = item.Address,
                ClaimCount = activityRecord.Data.ClaimCount+1
            });
            
            
        }
    }
    private async Task<TreeInfo> GetTreeGameTreeInfoAsync(int treeLevel)
    {
        //first join in game - init tree
        var treeLevels = GetTreeLevelInfoConfig();

        var treeInfo = new TreeInfo();
        var currentLevel = treeLevels.FirstOrDefault(x => x.Level == treeLevel);
        if (currentLevel == null)
        {
            throw new Exception("Invalid treelevel:"+treeLevel);
        }
        var nextLevel = treeLevels.FirstOrDefault(x => x.Level == (treeLevel+1));
        treeInfo.Current = currentLevel;
        treeInfo.Next = nextLevel;
        var nextLevelCost = 0;
        if (nextLevel != null)
        {
            nextLevelCost = nextLevel.MinPoints;
        }

        treeInfo.NextLevelCost = nextLevelCost;
        return treeInfo;
    }
    private List<TreeLevelInfo> GetTreeLevelInfoConfig()
    {
        var treeLevels = _platformOptionsMonitor.CurrentValue.TreeLevels;
        if (treeLevels.IsNullOrEmpty())
        {
            treeLevels = TreeGameConstants.TreeLevels;
        }

        return treeLevels;
    }
}