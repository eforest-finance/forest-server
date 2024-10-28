using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Chains;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.TreeGame.Provider;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Index;
using NFTMarketServer.Users.Provider;
using Org.BouncyCastle.Utilities;
using Orleans;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.TreeGame
{
    public class TreeGameService : NFTMarketServerAppService, ITreeGameService
    {
        private readonly INESTRepository<UserIndex, Guid> _userIndexRepository;
        private readonly INESTRepository<UserExtraIndex, Guid> _userExtraIndexRepository;
        private readonly IClusterClient _clusterClient;
        private readonly ILogger<UserAppService> _logger;
        private readonly IObjectMapper _objectMapper;
        private readonly IUserInformationProvider _userInformationProvider;
        private readonly IChainAppService _chainAppService;
        private readonly ISymbolIconAppService _symbolIconAppService;
        private readonly INESTRepository<UserIndex, Guid> _userRepository;
        private const string FullAddressPrefix = "ELF";
        public const char FullAddressSeparator = '_';
        private const string AELF = "AELF";
        private readonly ITreeGameUserInfoProvider _treeGameUserInfoProvider;
        private readonly IOptionsMonitor<TreeGameOptions> _platformOptionsMonitor;
        private readonly ITreeGamePointsDetailProvider _treeGamePointsDetailProvider;

        public TreeGameService(
            ITreeGameUserInfoProvider treeGameUserInfoProvider,
            ITreeGamePointsDetailProvider treeGamePointsDetailProvider,
            IUserInformationProvider userInformationProvider,
            INESTRepository<UserIndex, Guid> userIndexRepository,
            INESTRepository<UserExtraIndex, Guid> userExtraIndexRepository,
            ILogger<UserAppService> logger,
            IClusterClient clusterClient,
            IChainAppService chainAppService,
            ISymbolIconAppService symbolIconAppService,
            INESTRepository<UserIndex, Guid> userRepository,
            IObjectMapper objectMapper,
            IOptionsMonitor<TreeGameOptions> platformOptionsMonitor)

        {
            _userInformationProvider = userInformationProvider;
            _userIndexRepository = userIndexRepository;
            _userExtraIndexRepository = userExtraIndexRepository;
            _clusterClient = clusterClient;
            _logger = logger;
            _chainAppService = chainAppService;
            _objectMapper = objectMapper;
            _symbolIconAppService = symbolIconAppService;
            _userRepository = userRepository;
            _treeGameUserInfoProvider = treeGameUserInfoProvider;
            _platformOptionsMonitor = platformOptionsMonitor;
            _treeGamePointsDetailProvider = treeGamePointsDetailProvider;
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
            return await _treeGameUserInfoProvider.SaveOrUpdateTreeUserBalanceAsync(treeGameUserInfoDto);
        }

        private WaterInfoConfig GetWaterInfoConfig()
        {
            var waterInfoConfig = _platformOptionsMonitor.CurrentValue.WaterInfo ?? TreeGameConstants.WaterInfoConfig;
            return waterInfoConfig;
        }
        
        private List<TreeLevelInfo> GetTreeLevelInfoConfig()
        {
            var treeLevels = _platformOptionsMonitor.CurrentValue.TreeLevels;
            if (CollectionUtilities.IsNullOrEmpty(treeLevels))
            {
                treeLevels = TreeGameConstants.TreeLevels;
            }

            return treeLevels;
        }

        private async Task<WaterInfo> GetTreeGameWaterInfoAsync(TreeGameUserInfoIndex treeUserIndex)
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
                var currentActual = (complete * rtnWaterInfo.Produce) >= 60 ? 60 : (complete * rtnWaterInfo.Produce);
                if (currentActual != rtnWaterInfo.Current)
                {
                    rtnWaterInfo.Current = currentActual;
                    treeUserIndex.CurrentWater = (int)currentActual;
                    treeUserIndex.WaterUpdateTime = currentTime - remainder;
                    var treeGameUserInfoDto = _objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(treeUserIndex);
                    await _treeGameUserInfoProvider.SaveOrUpdateTreeUserBalanceAsync(treeGameUserInfoDto);
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
        public async Task GetTreeGamePointsDetailsAsync(string address, string nickName)
        {
            //first join in game - init pointsDetail
            var pointsDetails = await _treeGamePointsDetailProvider.GetTreePointsDetailsAsync(address);
            if (pointsDetails.IsNullOrEmpty())
            {
                var details = InitPointsDetailAsync(address);
            }

            //init pointsDetail
            return;
        }

        private List<TreeGamePointsDetailInfoIndex> InitPointsDetailAsync(string address)
        {
            var detailsConfig =  GetPointsDetailsConfig();
            for(var detail : detailsConfig){
                
            }
            return null;
        }

        public async Task<TreeGameHomePageInfoDto> GetUserTreeInfo(string address, string nickName)
        {
            var treeUserIndex = await _treeGameUserInfoProvider.GetTreeUserInfoAsync(address);
            if (treeUserIndex == null)
            {
                treeUserIndex = await InitNewTreeGameUserAsync(address, nickName);
            }
            //get points    
            var homePageDto = new TreeGameHomePageInfoDto();
            homePageDto.TotalPoints = treeUserIndex.Points;
            
            //get tree
            var treeInfo =  await GetTreeGameTreeInfoAsync(treeUserIndex.TreeLevel);
            homePageDto.TreeInfo = treeInfo;
            
            //get water
            var waterInfo = await GetTreeGameWaterInfoAsync(treeUserIndex);
            homePageDto.WaterInfo = waterInfo;
            
            //get points details
            return null;

            throw new NotImplementedException();
        }
    }
}