using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.Grains.Grain.Users;
using NFTMarketServer.Helper;
using NFTMarketServer.Users.Dto;
using NFTMarketServer.Users.Eto;
using NFTMarketServer.Users.Index;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Users.Provider;

public class UserInformationProvider : IUserInformationProvider, ISingletonDependency

{
    private readonly INESTRepository<UserIndex, Guid> _userIndexRepository;
    private readonly INESTRepository<UserExtraIndex, Guid> _userExtraIndexRepository;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public UserInformationProvider(INESTRepository<UserIndex, Guid> userIndexRepository,
        INESTRepository<UserExtraIndex, Guid> userExtraIndexRepository,
        IClusterClient clusterClient, IObjectMapper objectMapper, IDistributedEventBus distributedEventBus)
    {
        _userIndexRepository = userIndexRepository;
        _userExtraIndexRepository = userExtraIndexRepository;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<bool> CheckNameAsync(string name,Guid? userId)
    {
        if (string.IsNullOrEmpty(name))
        {
            return false;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Name).Terms(name)));
        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Must(mustQuery));
        var countResponse = await _userIndexRepository.GetAsync(Filter);
        if (countResponse == null ||  countResponse.Id == userId)
        {
            return true;
        }

        return false;
    }

    public async Task<UserDto> SaveUserSourceAsync(UserSourceInput userSourceInput)
    {
        var userGrain = _clusterClient.GetGrain<IUserGrain>(userSourceInput.UserId);
        var result = await userGrain.SaveUserSourceAsync(userSourceInput);
        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<UserGrainDto, UserInformationEto>(result.Data));
        return _objectMapper.Map<UserGrainDto, UserDto>(result.Data);
    }

    public async Task<UserIndex> GetByUserAddressAsync(string inputAddress)
    {
        if (inputAddress.IsNullOrWhiteSpace())
        {
            return null;
        }

        var shouldQuery = new List<Func<QueryContainerDescriptor<UserIndex>, QueryContainer>>();
        shouldQuery.Add(q => q.Terms(i => i.Field(f => f.AelfAddress).Terms(inputAddress)));
        shouldQuery.Add(q => q.Terms(i => i.Field(f => f.CaAddressMain).Terms(inputAddress)));
        shouldQuery.Add(q => q.Nested(i => i.Path("CaAddressListSide").Query(nq => nq
            .Terms(mm => mm
                .Field("CaAddressListSide.address")
                .Terms(inputAddress)
            )
         )));
        QueryContainer Filter(QueryContainerDescriptor<UserIndex> f) => f.Bool(b => b.Should(shouldQuery));

        return await _userIndexRepository.GetAsync(Filter);
    }
    
    public async Task<List<string>> GetFullAddressAsync(string inputAddress)
    {
        if (inputAddress.IsNullOrWhiteSpace())
        {
            return new List<string> { inputAddress };
        }
        
        var result = await GetByUserAddressAsync(inputAddress);
        if (result == null) return null;

        var sideChainIdList = result.CaAddressListSide?.Select(item => item.ChainId).ToList();
        
        var allAddress = new List<string>();
        if (!result.AelfAddress.IsNullOrEmpty())
        {
            allAddress.Add(FullAddressHelper.ToFullAddress(result.AelfAddress, CommonConstant.MainChainId));
            foreach (var chainId in sideChainIdList)
            {
                allAddress.Add(FullAddressHelper.ToFullAddress(result.AelfAddress, chainId));
            }
        }

        if (!result.CaAddressMain.IsNullOrEmpty())
        {
            allAddress.Add(FullAddressHelper.ToFullAddress(result.CaAddressMain, CommonConstant.MainChainId));
        }

        var caSideAddress = result.CaAddressListSide?.Select(e => FullAddressHelper.ToFullAddress(e.Address, e.ChainId))
            .ToList();
        if (!caSideAddress.IsNullOrEmpty())
        {
            allAddress.AddRange(caSideAddress);
        }

        return allAddress.IsNullOrEmpty() ? null : allAddress;
    }
    
    public async Task<List<string>> GetAllAddressAsync(string inputAddress)
    {
        if (inputAddress.IsNullOrWhiteSpace())
        {
            return new List<string> { inputAddress };
        }
        
        var result = await GetByUserAddressAsync(inputAddress);
        if (result == null) return null;

        var sideChainIdList = result.CaAddressListSide?.Select(item => item.ChainId).ToList();
        
        var allAddress = new List<string>();
        if (!result.AelfAddress.IsNullOrEmpty())
        {
            allAddress.Add(result.AelfAddress);
            foreach (var chainId in sideChainIdList)
            {
                allAddress.Add(result.AelfAddress);
            }
        }

        if (!result.CaAddressMain.IsNullOrEmpty())
        {
            allAddress.Add(result.CaAddressMain);
        }

        var caSideAddress = result.CaAddressListSide?.Select(e => e.Address)
            .ToList();
        if (!caSideAddress.IsNullOrEmpty())
        {
            allAddress.AddRange(caSideAddress);
        }

        return allAddress.IsNullOrEmpty() ? null : allAddress;
    }
    
    public async Task<UserIndex> GetByIdAsync(Guid id)
    {
        return await _userIndexRepository.GetAsync(id);
    }

    public async Task AddNewUserCountAsync(UserSourceInput userSourceInput, string userName)
    {
        UserExtraIndex userExtraIndex = new UserExtraIndex{
            Id = userSourceInput.UserId,
            UserName =userName,
            CaHash = userSourceInput.CaHash,
            AelfAddress = userSourceInput.AelfAddress,
            CaAddressMain = userSourceInput.CaAddressMain,
            CreateTime = DateTime.UtcNow
        };
        
        if (userSourceInput.CaAddressSide !=null)
        {
            List<UserAddress> userAddresses = new List<UserAddress>();
            foreach (var addressMap in userSourceInput.CaAddressSide)
            {
                UserAddress userAddress = new UserAddress
                {
                    ChainId = addressMap.Key,
                    Address = addressMap.Value
                };
                userAddresses.Add(userAddress);
            }

            userExtraIndex.CaAddressListSide = userAddresses;
        }
        
        await _userExtraIndexRepository.AddOrUpdateAsync(userExtraIndex);
    }
}