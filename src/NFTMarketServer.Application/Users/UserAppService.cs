using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Chains;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Helper;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Eto;
using NFTMarketServer.Users.Index;
using NFTMarketServer.Users.Provider;
using Orleans;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Users;

namespace NFTMarketServer.Users
{
    public class UserAppService : NFTMarketServerAppService, IUserAppService
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

        public UserAppService(
            IUserInformationProvider userInformationProvider,
            INESTRepository<UserIndex, Guid> userIndexRepository,
            INESTRepository<UserExtraIndex, Guid> userExtraIndexRepository,
            ILogger<UserAppService> logger,
            IClusterClient clusterClient,
            IChainAppService chainAppService,
            ISymbolIconAppService symbolIconAppService,
            INESTRepository<UserIndex, Guid> userRepository,
            IObjectMapper objectMapper)

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
        }

        public async Task<Dictionary<string, AccountDto>> GetAccountsAsync(List<string> addresses,
            string defaultChainId = null)
        {
            addresses = addresses.Where(addr => !addr.IsNullOrEmpty()).Distinct().ToList();
            if (addresses.IsNullOrEmpty()) return new();

            var addressSet = new HashSet<string>();
            var result = new Dictionary<string, AccountDto>();
            foreach (var address in addresses)
            {
                // input-address may be full-address
                var shortAddress = FullAddressHelper.ToShortAddress(address);
                if (addressSet.Add(shortAddress))
                {
                    result.Add(shortAddress, new AccountDto { Address = shortAddress, Name = shortAddress });
                }
            }

            var shouldQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>();
            shouldQuery.Add(q => q.Terms(i => i.Field(f => f.AelfAddress).Terms(addressSet)));
            shouldQuery.Add(q => q.Terms(i => i.Field(f => f.CaAddressMain).Terms(addressSet)));
            shouldQuery.Add(q => q.Nested(i => i.Path("CaAddressListSide").Query(nq => nq
                .Terms(mm => mm
                    .Field("CaAddressListSide.address")
                    .Terms(addressSet)
                )
            )));
            QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Should(shouldQuery));
            var users = await _userIndexRepository.GetListAsync(Filter);

            foreach (var addr in addressSet)
            {
                var user = MatchCpUser(users.Item2, addr);
                if (user != null)
                    result[addr] = MapAccount(user, addr);
            }

            return result;
        }

        private UserIndex MatchCpUser(List<UserIndex> userIndices, string address)
        {
            var user = userIndices.IsNullOrEmpty()
                ? null
                : userIndices
                    .Where(u =>
                        u.AelfAddress == address || u.CaAddressMain == address ||
                        u.CaAddressListSide != null && u.CaAddressListSide.Any(a => a.Address == address))
                    .FirstOrDefault((UserIndex)null);
            if (user == null) return null;
            
            var newUser = _objectMapper.Map<UserIndex, UserIndex>(user);
            if (newUser.Name.IsNullOrEmpty()) newUser.Name = address;
            
            return newUser;
        }

        public async Task<bool> CheckNameAsync(string name)
        {
            var userId = CurrentUser.GetId();
            return await _userInformationProvider.CheckNameAsync(name, userId);
        }

        public async Task UserUpdateAsync(UserUpdateDto input)
        {
            if (!await CheckNameAsync(input.Name))
            {
                throw new UserFriendlyException(message: "The name already used.");
            }
            var userGrain = _clusterClient.GetGrain<IUserGrain>(CurrentUser.GetId());
            var user = await userGrain.GetUserAsync();
            var userWaitUpdatedData = new UserGrainDto();
            if (input.UserUpdateType.Equals(UserUpdateType.ALL))
            {
                if (input.ProfileImage.IsNullOrEmpty())
                {
                    input.ProfileImage = await _symbolIconAppService.GetRandomImageAsync();
                }
                if (input.BannerImage.IsNullOrEmpty())
                {
                    input.BannerImage = CommonConstant.DefaultBannerImage;
                }
                userWaitUpdatedData = _objectMapper.Map(input, user.Data);
            }
            else if(input.UserUpdateType.Equals(UserUpdateType.BannerImage))
            {
                userWaitUpdatedData = user.Data;
                userWaitUpdatedData.BannerImage = input.BannerImage;
            }else if (input.UserUpdateType.Equals(UserUpdateType.ProfileImage))
            {
                userWaitUpdatedData = user.Data;
                userWaitUpdatedData.ProfileImage = input.ProfileImage;
            }
            
            var result = await userGrain.UpdateUserAsync(userWaitUpdatedData);
            if (!result.Success)
            {
                _logger.LogError("Update user information fail, UserId: {UserId}", CurrentUser.GetId());
                return;
            }

            var eventData = _objectMapper.Map<UserGrainDto, UserInformationEto>(result.Data);
            var userInfo = _objectMapper.Map<UserInformationEto, UserIndex>(eventData);
            if (eventData.CaAddressSide !=null)
            {
                List<UserAddress> userAddresses = new List<UserAddress>();
                foreach (var addressMap in eventData.CaAddressSide)
                {
                    UserAddress userAddress = new UserAddress
                    {
                        ChainId = addressMap.Key,
                        Address = addressMap.Value
                    };
                    userAddresses.Add(userAddress);
                }

                userInfo.CaAddressListSide = userAddresses;
            }
            await _userRepository.AddOrUpdateAsync(userInfo);
        }


        public async Task<UserDto> GetByUserAddressAsync(string inputAddress)
        {
            if (inputAddress.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("address is required.");
            }
            var user = await _userInformationProvider.GetByUserAddressAsync(inputAddress);
            if (user == null)
            {
                user = new UserIndex();
                user.ProfileImage = await _symbolIconAppService.GetRandomImageAsync();
                user.BannerImage = CommonConstant.DefaultBannerImage;
                user.AelfAddress = inputAddress;
            }

            if (user.ProfileImage.IsNullOrEmpty())
            {
                user.ProfileImage = await _symbolIconAppService.GetRandomImageAsync();
            }

            if (user.BannerImage.IsNullOrEmpty())
            {
                user.BannerImage = CommonConstant.DefaultBannerImage;
            }
            return user == null ? new UserDto() : MapUser(user, inputAddress);
        }


        private AccountDto MapAccount(UserIndex index, string address, string defaultChainId = null)
        {
            var user = ObjectMapper.Map<UserIndex, AccountDto>(index);

            user.Name = user.Name.IsNullOrEmpty() || user.Name == address 
                ? ToFullAddress(address, index, defaultChainId) 
                : user.Name;
            user.Address = address;

            if (!index.CaAddressMain.IsNullOrEmpty())
            {
                user.CaAddress[AELF] = index.CaAddressMain;
            }

            if (index.CaAddressListSide.IsNullOrEmpty())
            {
                return user;
            }

            foreach (var sideCaAddress in index.CaAddressListSide)
            {
                user.CaAddress[sideCaAddress.ChainId] = sideCaAddress.Address;
            }

            return user;
        }

        private string ToFullAddress(string address, UserIndex index = null, string defaultChainId = null)
        {
            return address.IsNullOrEmpty() ? address
                : FullAddressHelper.ToFullAddress(address, GetChainId(index, address, defaultChainId));
        }

        private UserDto MapUser(UserIndex index, string address)
        {
            var user = ObjectMapper.Map<UserIndex, UserDto>(index);
            user.Address = address;
            user.FullAddress = FullAddressHelper.ToFullAddress(address, GetChainId(index, address));

            return user;
        }

        private string GetChainId(UserIndex index, string address, string defaultChainId = null)
        {
            if (address.IsNullOrWhiteSpace())
            {
                return string.Empty;
            }

            if (!index.CaAddressListSide.IsNullOrEmpty())
            {
                foreach (var cAddress in index.CaAddressListSide)
                {
                    if (cAddress.Address.Equals(address))
                    {
                        return cAddress.ChainId;
                    }
                }
            }

            if (defaultChainId != null) return defaultChainId;

            // aelfAddress chainId‘s defaultValue is sideChainId
            if (address.Equals(index.AelfAddress))
            {
                Task<string[]> chainIds = _chainAppService.GetListAsync();
                if (!chainIds.Result.IsNullOrEmpty())
                {
                    foreach (var chainId in chainIds.Result)
                    {
                        if (!chainId.Equals(AELF))
                        {
                            return chainId;
                        }
                    }
                }
            }

            return AELF;
        }
        
        public async Task<long> GetUserCountAsync(DateTime beginTime, DateTime endTime)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<UserExtraIndex>, QueryContainer>>();
            
            mustQuery.Add(q=>
                q.DateRange(i =>
                    i.Field(f => f.CreateTime)
                        .GreaterThanOrEquals(beginTime).LessThan(endTime))
            );
            
            QueryContainer Filter(QueryContainerDescriptor<UserExtraIndex> f) => f.Bool(b => b.Must(mustQuery));
            
            var resp = await _userExtraIndexRepository.CountAsync(Filter);
            return resp.Count;
        }

        public async Task<string> GetCurrentUserAddressAsync()
        {
            var userGrain = _clusterClient.GetGrain<IUserGrain>(CurrentUser.GetId());
            var user = await userGrain.GetUserAsync();
            if (user?.Data == null)
            {
                throw new Exception("Please log in again");
            }
            
            _logger.LogInformation("GetCurrentUserAddressAsync grain:{}", JsonConvert.SerializeObject(user));

            var address = "";
            if (!user.Data.AelfAddress.IsNullOrEmpty())
            {
                address = user.Data.AelfAddress;
                return address;
            }
            if (!user.Data.CaAddressSide.IsNullOrEmpty())
            {
                address = user.Data.CaAddressSide.First().Value;
                return address;
            }
            if (!user.Data.CaAddressMain.IsNullOrEmpty())
            {
                address = user.Data.CaAddressMain;
                return address;
            }
            return address;
        }
        
        public async Task<string> TryGetCurrentUserAddressAsync()
        {
            if (CurrentUser == null || CurrentUser.Id == null || CurrentUser.GetId() == Guid.Empty)
            {
                return "";
            } 
            var userGrain = _clusterClient.GetGrain<IUserGrain>(CurrentUser.GetId());
            var user = await userGrain.GetUserAsync();
            if (user == null || user?.Data == null)
            {
                return "";
            }
            
            _logger.LogDebug("TryGetCurrentUserAddressAsync grain:{}", JsonConvert.SerializeObject(user));

            var address = "";
            if (!user.Data.AelfAddress.IsNullOrEmpty())
            {
                address = user.Data.AelfAddress;
                return address;
            }
            if (!user.Data.CaAddressSide.IsNullOrEmpty())
            {
                address = user.Data.CaAddressSide.First().Value;
                return address;
            }
            if (!user.Data.CaAddressMain.IsNullOrEmpty())
            {
                address = user.Data.CaAddressMain;
                return address;
            }
            return address;
        }
    }
}