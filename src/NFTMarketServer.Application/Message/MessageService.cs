using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Users;

namespace NFTMarketServer.Message;

public class MessageService: NFTMarketServerAppService, IMessageService
{
    private readonly IUserAppService _userAppService;
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly ILogger<MessageService> _logger;

    public MessageService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        ILogger<MessageService> logger,
        IUserAppService userAppService
    )
    {
        _userAppService = userAppService;
        _chainOptionsMonitor = chainOptionsMonitor;
        _logger = logger;
    }
    public async Task<List<MessageInfoDto>> GetMessageListAsync()
    {
        var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
        if (CollectionUtilities.IsNullOrEmpty(currentUserAddress))
        {
            throw new SystemException("Please log in and check the message list");
        }
        
        throw new System.NotImplementedException();
    }
}