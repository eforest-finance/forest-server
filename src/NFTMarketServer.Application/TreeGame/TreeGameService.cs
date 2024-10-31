using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Tree;
using NFTMarketServer.Tree.Provider;
using NFTMarketServer.TreeGame.Provider;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Index;
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


        public TreeGameService(
            ITreeActivityProvider treeActivityProvider,
            ITreeGameUserInfoProvider treeGameUserInfoProvider,
            ITreeGamePointsDetailProvider treeGamePointsDetailProvider,
            ILogger<TreeGameService> logger,
            IObjectMapper objectMapper,
            IOptionsMonitor<TreeGameOptions> platformOptionsMonitor,
            IUserAppService userAppService)

        {
            _logger = logger;
            _objectMapper = objectMapper;
            _treeGameUserInfoProvider = treeGameUserInfoProvider;
            _platformOptionsMonitor = platformOptionsMonitor;
            _treeGamePointsDetailProvider = treeGamePointsDetailProvider;
            _treeActivityProvider = treeActivityProvider;
            _userAppService = userAppService;
        }

        public async Task<TreeGameUserInfoIndex> InitNewTreeGameUserAsync(string address, string nickName)
        {
            //first join in game - init tree user
            var treeGameUserInfoDto = new TreeGameUserInfoDto()
            {
                Address = address,
                NickName = nickName,
                Points = 0,
                TreeLevel = GetTreeLevelInfoConfig().FirstOrDefault().Level,
                ParentAddress = "",
                CurrentWater = GetWaterInfoConfig().Max
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
                if (currentActual != rtnWaterInfo.Current)
                {
                    rtnWaterInfo.Current = (int)currentActual;
                    treeUserIndex.CurrentWater = (int)currentActual;
                    treeUserIndex.WaterUpdateTime = currentTime - remainder;
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
                    continue;
                }
                if (pointsDetail.TimeUnit == TimeUnit.MINUTE)
                {
                    var currentTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
                    var timeDiff = currentTime - pointsDetail.UpdateTime;
                    if ((timeDiff / 60000) < 1)
                    {
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
                await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(updateDetails);
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
                pointsDetailInfos.Add(new TreeGamePointsDetailInfoIndex()
                {
                    Id = Guid.NewGuid().ToString(),
                    Address = address,
                    Type = detail.Type,
                    Amount = treeInfo.Current.Produce,
                    UpdateTime = currentTime,
                    RemainingTime = treeInfo.Current.Frequency,
                    TimeUnit = detail.TimeUnit,
                    ClaimLimit = detail.ClaimLimit
                });
            }

            await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(pointsDetailInfos);
            return pointsDetailInfos;
        }

        public async Task<TreeGameHomePageInfoDto> GetUserTreeInfoAsync(string address, string nickName, bool needStorage)
        {
            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex == null)
            {
                treeUserIndex = await InitNewTreeGameUserAsync(address, nickName);
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

        public async Task<TreeGameHomePageInfoDto> WateringTreeAsync(string address, int count)
        {
            var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress != address)
            {
                throw new Exception("Login address and parameter address are inconsistent");
            }

            var needStorage = false;
            if (count != 1)
            {
                throw new Exception("Invalid param count");
            }

            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex == null)
            {
                throw new Exception("Please refresh homepage, init your tree");
            }
            
            //cal water
            var waterInfo = await GetAndRefreshTreeGameWaterInfoAsync(treeUserIndex, needStorage);
            if ((waterInfo.Current - count) < 0)
            {
                throw new Exception("You don't have enough water");
            }

            waterInfo.Current = waterInfo.Current - count;
            waterInfo.UpdateTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            treeUserIndex.CurrentWater = waterInfo.Current - count;
            treeUserIndex.WaterUpdateTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
            
            //cal points detail
            var treeInfo =  await GetTreeGameTreeInfoAsync(treeUserIndex.TreeLevel);
            var pointsDetails = await GetAndRefreshTreeGamePointsDetailsAsync(address, treeInfo, needStorage);

            var updateDetails = new List<TreeGamePointsDetailInfoIndex>();
            foreach (var pointsDetail in pointsDetails)
            {
                if (pointsDetail.Type == PointsDetailType.INVITE)
                {
                    continue;
                }

                if (pointsDetail.RemainingTime <= 0)
                {
                    continue;
                }

                if (pointsDetail.TimeUnit != TimeUnit.MINUTE)
                {
                    throw new Exception("Invalid pointsDetail timeunit:" + pointsDetail.TimeUnit);
                }

                var remainingTime = pointsDetail.RemainingTime - count*waterInfo.WateringIncome;
                pointsDetail.RemainingTime = remainingTime;
                updateDetails.Add(_objectMapper.Map<PointsDetail, TreeGamePointsDetailInfoIndex>(pointsDetail));
            }
            
            //build rtun msg
            var homePageDto = new TreeGameHomePageInfoDto();
            homePageDto.TotalPoints = treeUserIndex.Points;
            homePageDto.TreeInfo = treeInfo;
            homePageDto.WaterInfo = waterInfo;
            homePageDto.PointsDetails = pointsDetails;
            
            //update db
            await _treeGameUserInfoProvider.SaveOrUpdateTreeUserInfoAsync(_objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(treeUserIndex));
            await _treeGamePointsDetailProvider.BulkSaveOrUpdateTreePointsDetailsAsync(updateDetails);
            return homePageDto;
        }

        public async Task<TreeLevelUpgradeOutput> UpgradeTreeLevelAsync(string address, int nextLevel)
        {
            var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress != address)
            {
                throw new Exception("Login address and parameter address are inconsistent");
            }
            
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
            var requestHash = BuildRequestHash(string.Concat(address, treeInfo.NextLevelCost, opTime));
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

        public async Task<TreePointsClaimOutput> ClaimAsync(string address, PointsDetailType pointsDetailType)
        {
            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex == null)
            {
                throw new Exception("Please refresh homepage, init your tree");
            }
            var treeInfo =  await GetTreeGameTreeInfoAsync(treeUserIndex.TreeLevel);
            var pointsDetails = await GetAndRefreshTreeGamePointsDetailsAsync(address, treeInfo, false);
            var claimPointsDetail = new PointsDetail();
            claimPointsDetail = null;
            var claimPointsAmount = 0l;

            foreach (var pointsDetail in pointsDetails)
            {
                if (pointsDetail.Type == pointsDetailType && 
                    (pointsDetail.Type == PointsDetailType.NORMALONE ||pointsDetail.Type == PointsDetailType.NORMALTWO) 
                    && pointsDetail.RemainingTime <= 0)
                {
                    claimPointsDetail = pointsDetail;
                    claimPointsAmount = treeInfo.Current.Produce;
                    claimPointsDetail.Amount = claimPointsAmount;
                }
                if (pointsDetail.Type == pointsDetailType && 
                    (pointsDetail.Type == PointsDetailType.INVITE) 
                    && pointsDetail.Amount >= pointsDetail.ClaimLimit)
                {
                    claimPointsDetail = pointsDetail;
                    claimPointsAmount = pointsDetail.Amount;
                    claimPointsDetail.Amount = 0;
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
            var requestHash = BuildRequestHash(string.Concat(address, pointsDetailType, claimPointsAmount, opTime));
            var response = new TreePointsClaimOutput()
            {
                Address = address,
                PointsDetailType = pointsDetailType,
                Points = claimPointsAmount,
                OpTime = opTime,
                RequestHash = requestHash
            };
            return response;
        }

        public async Task<TreePointsConvertOutput> PointsConvertAsync(string address, string activityId)
        {
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
            var requestHash = BuildRequestHash(string.Concat(address, activityId, costPoints,opTime));
            var response = new TreePointsConvertOutput()
            {
                Address = address,
                ActivityId = activityId,
                Points = costPoints,
                OpTime = opTime,
                RequestHash = requestHash
            };

            return response;
        }

        private string BuildRequestHash(string request)
        {
            var hashVerifyKey = _platformOptionsMonitor.CurrentValue.HashVerifyKey ?? TreeGameConstants.HashVerifyKey;
            var requestHash = HashHelper.ComputeFrom(string.Concat(request, hashVerifyKey));
            return requestHash.ToHex();
        }
    }
}