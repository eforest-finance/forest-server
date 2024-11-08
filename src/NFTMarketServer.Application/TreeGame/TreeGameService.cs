using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using MassTransit.Mediator;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Tree;
using NFTMarketServer.Tree.Provider;
using NFTMarketServer.TreeGame.Provider;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Index;
using Volo.Abp.Caching;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.TreeGame
{
    public class TreeGameService : NFTMarketServerAppService, ITreeGameService
    {
        private readonly ITreeActivityProvider _treeActivityProvider;
        private readonly ILogger<TreeGameService> _logger;
        private readonly IObjectMapper _objectMapper;
        private readonly ITreeGameUserInfoProvider _treeGameUserInfoProvider;
        private readonly IOptionsMonitor<TreeGameOptions> _platformOptionsMonitor;
        private readonly ITreeGamePointsDetailProvider _treeGamePointsDetailProvider;
        private readonly IUserAppService _userAppService;
        private readonly IDistributedCache<TreeClaimPointsCacheItem> _claimPointsCache;
        private const string ClaimCatchKeyPrefix = "claim_points_";
        private const long ClaimCatchExpireTime = 30000;//30s



        public TreeGameService(
            ITreeActivityProvider treeActivityProvider,
            ITreeGameUserInfoProvider treeGameUserInfoProvider,
            ITreeGamePointsDetailProvider treeGamePointsDetailProvider,
            ILogger<TreeGameService> logger,
            IObjectMapper objectMapper,
            IOptionsMonitor<TreeGameOptions> platformOptionsMonitor,
            IUserAppService userAppService,
            IDistributedCache<TreeClaimPointsCacheItem> claimPointsCache)

        {
            _logger = logger;
            _objectMapper = objectMapper;
            _treeGameUserInfoProvider = treeGameUserInfoProvider;
            _platformOptionsMonitor = platformOptionsMonitor;
            _treeGamePointsDetailProvider = treeGamePointsDetailProvider;
            _treeActivityProvider = treeActivityProvider;
            _userAppService = userAppService;
            _claimPointsCache = claimPointsCache;

        }

        public async Task<TreeGameUserInfoIndex> InitNewTreeGameUserAsync(string address, string nickName,string parentAddress)
        {
            //first join in game - init tree user
            var treeGameUserInfoDto = new TreeGameUserInfoDto()
            {
                Id = Guid.NewGuid().ToString(),
                Address = address,
                NickName = nickName,
                Points = 0,
                TreeLevel = GetTreeLevelInfoConfig().FirstOrDefault().Level,
                ParentAddress = parentAddress,
                CurrentWater = GetWaterInfoConfig().Max,
                CreateTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                WaterUpdateTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            };
            return await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(treeGameUserInfoDto);
        }

        private WaterInfoConfig GetWaterInfoConfig()
        {
            var waterInfoConfig = _platformOptionsMonitor.CurrentValue.WaterInfo ?? TreeGameConstants.WaterInfoConfig;
            return waterInfoConfig;
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

        private async Task<WaterInfo> GetAndRefreshTreeGameWaterInfoAsync(TreeGameUserInfoIndex treeUserIndex, bool needStorage)
        {
            //first join in game - init water
                var waterConfig = GetWaterInfoConfig();

            var rtnWaterInfo = new WaterInfo()
            {
                Current = waterConfig.Max,
                Max = waterConfig.Max,
                Produce = waterConfig.Produce,
                Frequency = waterConfig.Frequency,
                WateringIncome = waterConfig.WateringIncome,
                TimeUnit = waterConfig.TimeUnit
            };
            
            //get user water
            rtnWaterInfo.Current = treeUserIndex.CurrentWater;
            rtnWaterInfo.UpdateTime = treeUserIndex.WaterUpdateTime;
            var currentTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            var timeDiff = currentTime - rtnWaterInfo.UpdateTime;
            if (rtnWaterInfo.TimeUnit == TimeUnit.MINUTE)
            {
                var complete = timeDiff / (rtnWaterInfo.Frequency * 60 * 1000);
                var remainder = timeDiff % (rtnWaterInfo.Frequency * 60 * 1000);
                var currentActual = (treeUserIndex.CurrentWater + complete * rtnWaterInfo.Produce) >= 60 ? 60 : (treeUserIndex.CurrentWater + complete * rtnWaterInfo.Produce);
                if (treeUserIndex.CurrentWater == 60)
                {
                    treeUserIndex.WaterUpdateTime = currentTime;
                    rtnWaterInfo.UpdateTime = currentTime;
                    if (needStorage)
                    {
                        var treeGameUserInfoDto = _objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(treeUserIndex);
                        await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(treeGameUserInfoDto);
                    }
                }else if (currentActual != rtnWaterInfo.Current)
                {
                    rtnWaterInfo.Current = (int)currentActual;
                    treeUserIndex.CurrentWater = (int)currentActual;
                    treeUserIndex.WaterUpdateTime = currentTime - remainder;
                    rtnWaterInfo.UpdateTime = treeUserIndex.WaterUpdateTime;
                    if (needStorage)
                    {
                        var treeGameUserInfoDto = _objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(treeUserIndex);
                        await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(treeGameUserInfoDto);
                    }
                }
            }
            else
            {
                throw new Exception("Invalid water timeunit:"+ rtnWaterInfo.TimeUnit);
            }

            return rtnWaterInfo;
        }
        public async Task<TreeInfo> GetTreeGameTreeInfoAsync(int treeLevel)
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
        private List<PointsDetailConfig> GetPointsDetailsConfig()
        {
            var pointsDetailsConfig = _platformOptionsMonitor.CurrentValue.PointsDetails ?? TreeGameConstants.PointsDetailConfig;
            return pointsDetailsConfig;
        }

        private async Task<List<PointsDetail>> GetAndRefreshTreeGamePointsDetailsAsync(string address, TreeInfo treeInfo, bool needStorage)
        {
            //first join in game - init pointsDetail
            var pointsDetailInfos = await _treeGamePointsDetailProvider.GetTreePointsDetailsAsync(address);
            if (pointsDetailInfos.IsNullOrEmpty())
            {
                pointsDetailInfos = await InitPointsDetailAsync(address, treeInfo);
                return pointsDetailInfos.Select(i => _objectMapper.Map<TreeGamePointsDetailInfoIndex, PointsDetail>(i)).ToList();
            }

            var updateDetails = new List<TreeGamePointsDetailInfoIndex>();
            foreach (var pointsDetail in pointsDetailInfos)
            {
                if (pointsDetail.RemainingTime == 0)
                {
                    updateDetails.Add(pointsDetail);
                    continue;
                }
                if (pointsDetail.TimeUnit == TimeUnit.MINUTE)
                {
                    var currentTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
                    var timeDiff = currentTime - pointsDetail.UpdateTime;
                    if ((timeDiff / 60000) < 1)
                    {
                        updateDetails.Add(pointsDetail);
                        continue;
                    }

                    var remainingTime =pointsDetail.RemainingTime - (timeDiff/60000);
                    pointsDetail.RemainingTime = remainingTime <= 0 ? 0 : remainingTime;
                    pointsDetail.UpdateTime = currentTime;
                    updateDetails.Add(pointsDetail);
                }
                else
                {
                    throw new Exception("Invalid pointsDetail timeunit:"+ pointsDetail.TimeUnit);
                }
            }

            if (needStorage && !updateDetails.IsNullOrEmpty())
            {
                await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(address, updateDetails);
            }
            return pointsDetailInfos.Select(i => _objectMapper.Map<TreeGamePointsDetailInfoIndex, PointsDetail>(i)).ToList();
        }

        private async Task<List<TreeGamePointsDetailInfoIndex>> InitPointsDetailAsync(string address, TreeInfo treeInfo)
        {
            var detailsConfig =  GetPointsDetailsConfig();
            List<TreeGamePointsDetailInfoIndex> pointsDetailInfos = new List<TreeGamePointsDetailInfoIndex>();
            var currentTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            foreach (var detail in detailsConfig)
            {
                var amount = 0l;
                if (detail.Type != PointsDetailType.INVITE)
                {
                    amount = treeInfo.Current.Produce;
                }

                pointsDetailInfos.Add(new TreeGamePointsDetailInfoIndex()
                {
                    Id = Guid.NewGuid().ToString(),
                    Address = address,
                    Type = detail.Type,
                    Amount = amount,
                    UpdateTime = currentTime,
                    RemainingTime = treeInfo.Current.Frequency,
                    TimeUnit = detail.TimeUnit,
                    ClaimLimit = detail.ClaimLimit
                });
            }

            await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(address, pointsDetailInfos);
            return pointsDetailInfos;
        }

        public async Task<TreeGameHomePageInfoDto> GetUserTreeInfoAsync(string address, string nickName, bool needStorage, string parentAddress)
        {
            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex == null)
            {
                treeUserIndex = await InitNewTreeGameUserAsync(address, nickName, parentAddress);
            }
            //get points    
            var homePageDto = new TreeGameHomePageInfoDto();
            homePageDto.TotalPoints = treeUserIndex.Points;
            homePageDto.Id = treeUserIndex.Id;
            homePageDto.Address = treeUserIndex.Address;
            homePageDto.NickName = treeUserIndex.NickName;
            //get tree
            var treeInfo =  await GetTreeGameTreeInfoAsync(treeUserIndex.TreeLevel);
            homePageDto.TreeInfo = treeInfo;
            
            //get water
            var waterInfo = await GetAndRefreshTreeGameWaterInfoAsync(treeUserIndex, needStorage);
            homePageDto.WaterInfo = waterInfo;
            
            //get points details
            var pointsDetails = await GetAndRefreshTreeGamePointsDetailsAsync(address, treeInfo, needStorage);
            homePageDto.PointsDetails = pointsDetails;
            return homePageDto;
        }

        public async Task<TreeGameHomePageInfoDto> WateringTreeAsync(TreeWateringRequest input)
        {
            var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress != input.Address)
            {
                throw new Exception("Login address and parameter address are inconsistent");
            }
            var needStorage = false;
            if (input.Count != 1)
            {
                throw new Exception("Invalid param count");
            }

            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(currentUserAddress);
            if (treeUserIndex == null)
            {
                throw new Exception("Please refresh homepage, init your tree");
            }
            
            //cal water
            var waterInfo = await GetAndRefreshTreeGameWaterInfoAsync(treeUserIndex, needStorage);
            if ((waterInfo.Current - input.Count) < 0)
            {
                throw new Exception("You don't have enough water");
            }

            waterInfo.Current = waterInfo.Current - input.Count;
            treeUserIndex.CurrentWater = waterInfo.Current;
            treeUserIndex.WaterUpdateTime = waterInfo.UpdateTime;
            
            //cal points detail
            var treeInfo =  await GetTreeGameTreeInfoAsync(treeUserIndex.TreeLevel);
            var pointsDetails = await GetAndRefreshTreeGamePointsDetailsAsync(currentUserAddress, treeInfo, needStorage);

            var updateDetails = new List<TreeGamePointsDetailInfoIndex>();
            bool hasWateringOneDetail = false;
            foreach (var pointsDetail in pointsDetails)
            {
                if (pointsDetail.Type == PointsDetailType.INVITE)
                {
                    updateDetails.Add(_objectMapper.Map<PointsDetail, TreeGamePointsDetailInfoIndex>(pointsDetail));
                    continue;
                }

                if (pointsDetail.RemainingTime <= 0)
                {
                    updateDetails.Add(_objectMapper.Map<PointsDetail, TreeGamePointsDetailInfoIndex>(pointsDetail));
                    continue;
                }

                if (hasWateringOneDetail)
                {
                    updateDetails.Add(_objectMapper.Map<PointsDetail, TreeGamePointsDetailInfoIndex>(pointsDetail));
                    continue;
                }

                if (pointsDetail.TimeUnit != TimeUnit.MINUTE)
                {
                    throw new Exception("Invalid pointsDetail timeunit:" + pointsDetail.TimeUnit);
                }

                var remainingTime = pointsDetail.RemainingTime - input.Count*waterInfo.WateringIncome;
                pointsDetail.RemainingTime = remainingTime<0?0:remainingTime;
                updateDetails.Add(_objectMapper.Map<PointsDetail, TreeGamePointsDetailInfoIndex>(pointsDetail));
                hasWateringOneDetail = true;
            }
            
            //build rtun msg
            var homePageDto = new TreeGameHomePageInfoDto();
            homePageDto.TotalPoints = treeUserIndex.Points;
            homePageDto.Address = treeUserIndex.Address;
            homePageDto.Id = treeUserIndex.Id;
            homePageDto.NickName = treeUserIndex.NickName;
            homePageDto.TreeInfo = treeInfo;
            homePageDto.WaterInfo = waterInfo;
            homePageDto.PointsDetails = pointsDetails;
            
            //update db
            await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(_objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(treeUserIndex));
            await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(input.Address, updateDetails);
            return homePageDto;
        }

        public async Task<TreeLevelUpgradeOutput> UpgradeTreeLevelAsync(TreeLevelUpdateRequest request)
        {
            /*var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress != address)
            {
                throw new Exception("Login address and parameter address are inconsistent");
            }*/
            var address = request.Address;
            var nextLevel = request.NextLevel;
            var currentUserAddress = address;
            
            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex == null)
            {
                throw new Exception("Please refresh homepage, init your tree");
            }
            var treeInfo =  await GetTreeGameTreeInfoAsync(treeUserIndex.TreeLevel);
            if (treeInfo.Next.Level != nextLevel)
            {
                throw new Exception("Tree level parameter error, currentLevel:"+treeInfo.Current.Level+",nextLevel:"+treeInfo.Next.Level);
            }

            if (treeUserIndex.Points < treeInfo.NextLevelCost)
            {
                throw new Exception("You don't have enough points");
            }
            
            //build requestHash
            var opTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            var requestHash = BuildRequestHash(string.Concat(address, treeInfo.NextLevelCost, opTime, treeInfo.Next.Level));
            var response = new TreeLevelUpgradeOutput()
            {
                Address = address,
                Points = treeInfo.NextLevelCost,
                OpTime = opTime,
                UpgradeLevel = treeInfo.Next.Level,
                RequestHash = requestHash
            };
            return response;
        }

        private async Task<bool> CheckTreeClaimPointsCacheAsync(string address, PointsDetailType type)
        {
            var cacheKey = ClaimCatchKeyPrefix + address + "_" +type;
            var treeClaimPointsCacheItem = await _claimPointsCache.GetAsync(cacheKey);
            if (treeClaimPointsCacheItem == null)
            {
                return true;
            }
            var currentTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            if ((currentTime - treeClaimPointsCacheItem.ClaimTime) >= ClaimCatchExpireTime)
            {
                return true;
            }
            return false;
        }
        private async Task SetTreeClaimPointsCacheAsync(string address,PointsDetailType type, long time)
        {
            var cacheKey = ClaimCatchKeyPrefix + address + "_" + type;
            var cacheOptions = new DistributedCacheEntryOptions
            {
                SlidingExpiration = TimeSpan.FromSeconds(ClaimCatchExpireTime/1000)
            };
            var treeClaimPointsCacheItem = new TreeClaimPointsCacheItem()
            {
                ClaimTime = time
            };
            await _claimPointsCache.SetAsync(cacheKey, treeClaimPointsCacheItem, cacheOptions);
        }

        public async Task<TreePointsClaimOutput> ClaimAsync(TreePointsClaimRequest request)
        {
            var checkCache = await CheckTreeClaimPointsCacheAsync(request.Address, request.PointsDetailType);
            if (!checkCache)
            {
                throw new Exception(
                    "You have already extracted it, please be patient and wait for the confirmation of the results on the chain.");
            }
            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(request.Address);
            if (treeUserIndex == null)
            {
                throw new Exception("Please refresh homepage, init your tree");
            }
            var treeInfo =  await GetTreeGameTreeInfoAsync(treeUserIndex.TreeLevel);
            var pointsDetails = await GetAndRefreshTreeGamePointsDetailsAsync(request.Address, treeInfo, false);
            var claimPointsDetail = new PointsDetail();
            claimPointsDetail = null;
            var claimPointsAmount = 0l;

            foreach (var pointsDetail in pointsDetails)
            {
                if (pointsDetail.Type == request.PointsDetailType && 
                    (pointsDetail.Type == PointsDetailType.NORMALONE ||pointsDetail.Type == PointsDetailType.NORMALTWO) 
                    && pointsDetail.RemainingTime <= 0)
                {
                    claimPointsDetail = pointsDetail;
                    claimPointsAmount = treeInfo.Current.Produce;
                    claimPointsDetail.Amount = claimPointsAmount;
                    break;
                }
                if (pointsDetail.Type == request.PointsDetailType && 
                    (pointsDetail.Type == PointsDetailType.INVITE) 
                    && pointsDetail.Amount >= pointsDetail.ClaimLimit)
                {
                    claimPointsDetail = pointsDetail;
                    claimPointsAmount = pointsDetail.Amount;
                    claimPointsDetail.Amount = 0;
                    break;
                }
            }

            if (claimPointsDetail == null)
            {
                throw new Exception("Your fruit does not meet the extraction criteria");
            }

            /*//update db
            treeUserIndex.Points += claimPointsAmount;
            await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(_objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(treeUserIndex));
            var pointsDetailIndex = _objectMapper.Map<PointsDetail, TreeGamePointsDetailInfoIndex>(claimPointsDetail);
            await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetaislAsync(new List<TreeGamePointsDetailInfoIndex>(){pointsDetailIndex});*/
            //build requestHash
            var opTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            var requestHash = BuildRequestHash(string.Concat(request.Address, claimPointsAmount,(int)request.PointsDetailType, opTime));
            await SetTreeClaimPointsCacheAsync(request.Address, request.PointsDetailType, opTime);
            var response = new TreePointsClaimOutput()
            {
                Address = request.Address,
                PointsType = request.PointsDetailType,
                Points = claimPointsAmount,
                OpTime = opTime,
                RequestHash = requestHash
            };
            return response;
        }

        public async Task<TreePointsConvertOutput> PointsConvertAsync(TreePointsConvertRequest request)
        {
            var address = request.Address;
            var activityId = request.ActivityId;
            var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress != address)
            {
                throw new Exception("Login address and parameter address are inconsistent");
            }
            
            var activityDetail = await  _treeActivityProvider.GetTreeActivityDetailAsync(activityId);
            if (activityDetail == null)
            {
                throw new Exception("Invalid activityId");
            }
            
            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex == null)
            {
                throw new Exception("Please refresh homepage, init your tree");
            }

            var activityType = activityDetail.RedeemType;
            var costPoints = activityType == RedeemType.Free?0:activityDetail.CostPoints;
            if (treeUserIndex.Points < activityDetail.MinPoints || treeUserIndex.Points < costPoints)
            {
                throw new Exception("you do not have enough points");
            }

            var opTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            var rewardSymbol = activityDetail.RewardType.ToString();
            var rewardAmount = activityDetail.RedeemRewardOnce;
            var rewardDecimals = GetSymbolDecimals(rewardSymbol);
            rewardAmount =  (long)(rewardAmount * (decimal)Math.Pow(10, rewardDecimals));
            var requestStr = string.Concat(address, activityId, costPoints,opTime);
            var requestHash = BuildRequestHash(string.Concat(requestStr,rewardSymbol,rewardAmount));
            
            var response = new TreePointsConvertOutput()
            {
                Address = address,
                ActivityId = activityId,
                Points = costPoints,
                OpTime = opTime,
                RewardSymbol = rewardSymbol,
                RewardAmount = (long)rewardAmount,
                RequestHash = requestHash,
            };

            return response;
        }

        public async Task<List<string>> GetInviteFriendsAsync(string address)
        {
            var friends = await _treeGameUserInfoProvider.GetTreeUsersByParentUserAsync(address);
            if (friends == null || friends.Item1 == 0 || friends.Item2.IsNullOrEmpty())
            {
                return new List<string>();
            }
            return friends.Item2.Select(i => i.NickName).ToList();
        }

        private int GetSymbolDecimals(string symbol)
        {
            var rewardsConfig = _platformOptionsMonitor.CurrentValue.Rewards ?? TreeGameConstants.RewardsConfig;
            if (rewardsConfig.IsNullOrEmpty())
            {
                return TreeGameConstants.DefaultRewardDecimal;
            }

            var reward = rewardsConfig.FirstOrDefault(x => x.Symbol == symbol);
            return reward?.Decimals ?? TreeGameConstants.DefaultRewardDecimal;
        }

        private string BuildRequestHash(string request)
        {
            var hashVerifyKey = _platformOptionsMonitor.CurrentValue.HashVerifyKey ?? TreeGameConstants.HashVerifyKey;
            var requestHash = HashHelper.ComputeFrom(string.Concat(request, hashVerifyKey));
            return requestHash.ToHex();
        }

        public async Task AcceptInvitationAsync(string address, string nickName, string parentAddress)
        {
            _logger.LogInformation("miniapp AcceptInvitationAsync address:{A} nickName:{nickName} parentAddress:{parentAddress}",address,nickName,parentAddress);

            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex != null)
            {
                _logger.LogInformation("miniapp AcceptInvitationAsync address:{A} tree already exists nickName:{nickName} parentAddress:{parentAddress}",address,nickName,parentAddress);
                return;
            }
            //init user tree
            var treeGameHomePageInfoDto = await GetUserTreeInfoAsync(address, nickName, false, parentAddress);

            var pointsDetails = treeGameHomePageInfoDto.PointsDetails;
            var pointsDetailsInfos = new List<TreeGamePointsDetailInfoIndex>();
            foreach (var detail in pointsDetails)
            {
                if (detail.Type == PointsDetailType.INVITE)
                {
                    detail.Amount += TreeGameConstants.TreeGameInviteReward;
                }
                pointsDetailsInfos.Add(_objectMapper.Map<PointsDetail,TreeGamePointsDetailInfoIndex>(detail));
            }
            await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(address, pointsDetailsInfos);
        }

    }
}