using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.Users.Eto;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.EntityEventHandler.Core;

public class UserHandler : IDistributedEventHandler<UserInformationEto>, ITransientDependency
{
    private readonly INESTRepository<UserIndex, Guid> _userRepository;
    private readonly IObjectMapper _objectMapper;
    private readonly ILogger<UserHandler> _logger;

    public UserHandler(
        INESTRepository<UserIndex, Guid> userRepository,
        IObjectMapper objectMapper,
        ILogger<UserHandler> logger)
    {
        _userRepository = userRepository;
        _objectMapper = objectMapper;
        _logger = logger;
    }
    [ExceptionHandler(typeof(Exception),
        Message = "UserHandler.HandleEventAsync User information add or update fail:", 
        LogOnly = true,
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        LogTargets = new []{"eventData" }
    )]
    public virtual async Task HandleEventAsync(UserInformationEto eventData)
    {
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

        if (userInfo != null)
        {
            _logger.LogDebug("User information add or update success: {userInformation}",
                JsonConvert.SerializeObject(userInfo));
        }
    }
}