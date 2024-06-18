using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Volo.Abp.ObjectMapping;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Basic;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Message.Provider;
using NFTMarketServer.Users;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Message;
[RemoteService(IsEnabled = false)]

public class MessageService: NFTMarketServerAppService, IMessageService
{
    private readonly IUserAppService _userAppService;
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly ILogger<MessageService> _logger;
    private readonly IMessageInfoProvider _messageInfoProvider;
    private readonly IObjectMapper _objectMapper;

    public MessageService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        ILogger<MessageService> logger,
        IUserAppService userAppService,
        IMessageInfoProvider messageInfoProvider,
        IObjectMapper objectMapper
    )
    {
        _userAppService = userAppService;
        _chainOptionsMonitor = chainOptionsMonitor;
        _logger = logger;
        _messageInfoProvider = messageInfoProvider;
        _objectMapper = objectMapper;

    }
    public async Task<PagedResultDto<MessageInfoDto>> GetMessageListAsync(QueryMessageListInput input)
    {
        var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
        if (currentUserAddress.IsNullOrEmpty())
        {
            return new PagedResultDto<MessageInfoDto>()
            {
                Items = new List<MessageInfoDto>()
            };
        }

        input.Status = CommonConstant.MessageUnReadStatus;
        var result = await _messageInfoProvider.GetUserMessageInfosAsync(currentUserAddress, input);
        if (result == null || result.Item1 <= CommonConstant.IntZero)
        {
            return new PagedResultDto<MessageInfoDto>()
            {
                TotalCount = CommonConstant.IntZero,
                Items = new List<MessageInfoDto>()
            };
        }
        return new PagedResultDto<MessageInfoDto>()
        {
            TotalCount = result.Item1,
            Items = _objectMapper.Map<List<MessageInfoIndex>, List<MessageInfoDto>>(result.Item2)
        };
    }
}