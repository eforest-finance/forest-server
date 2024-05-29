using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.Chains;
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
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly ILogger<UserAppService> _logger;
        private readonly IObjectMapper _objectMapper;
        private readonly IUserInformationProvider _userInformationProvider;
        private readonly IChainAppService _chainAppService;
        private const string FullAddressPrefix = "ELF";
        public const char FullAddressSeparator = '_';
        private const string AELF = "AELF";

        public UserAppService(
            IUserInformationProvider userInformationProvider,
            INESTRepository<UserIndex, Guid> userIndexRepository,
            INESTRepository<UserExtraIndex, Guid> userExtraIndexRepository,
            ILogger<UserAppService> logger,
            IDistributedEventBus distributedEventBus,
            IClusterClient clusterClient,
            IChainAppService chainAppService,
            IObjectMapper objectMapper)

        {
            _userInformationProvider = userInformationProvider;
            _userIndexRepository = userIndexRepository;
            _userExtraIndexRepository = userExtraIndexRepository;
            _clusterClient = clusterClient;
            _logger = logger;
            _distributedEventBus = distributedEventBus;
            _chainAppService = chainAppService;
            _objectMapper = objectMapper;
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
            var userWaitUpdatedData = _objectMapper.Map(input, user.Data);
            
            var result = await userGrain.UpdateUserAsync(userWaitUpdatedData);
            if (!result.Success)
            {
                _logger.LogError("Update user information fail, UserId: {UserId}", CurrentUser.GetId());
                return;
            }
            
            await _distributedEventBus.PublishAsync(_objectMapper.Map<UserGrainDto, UserInformationEto>(result.Data));
        }


        public async Task<UserDto> GetByUserAddressAsync(string inputAddress)
        {
            if (inputAddress.IsNullOrWhiteSpace())
            {
                throw new UserFriendlyException("address is required.");
            }
            var user = await _userInformationProvider.GetByUserAddressAsync(inputAddress);
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