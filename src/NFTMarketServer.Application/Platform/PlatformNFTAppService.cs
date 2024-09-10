using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Ai;
using NFTMarketServer.Basic;
using NFTMarketServer.Common.AElfSdk;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.Users;
using Orleans;
using Volo.Abp;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Platform;

[RemoteService(IsEnabled = false)]
public class PlatformNFTAppService : NFTMarketServerAppService, IPlatformNFTAppService
{
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly IOptionsMonitor<PlatformNFTOptions> _platformOptionsMonitor;
    private readonly ILogger<PlatformNFTAppService> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly IUserAppService _userAppService;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;


    public PlatformNFTAppService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<PlatformNFTOptions> platformOptionsMonitor,
        ILogger<PlatformNFTAppService> logger,
        IContractProvider contractProvider,
        IUserAppService userAppService,
        IObjectMapper objectMapper,
        IClusterClient clusterClient
    )
    {
        _chainOptionsMonitor = chainOptionsMonitor;
        _platformOptionsMonitor = platformOptionsMonitor;
        _logger = logger;
        _contractProvider = contractProvider;
        _userAppService = userAppService;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;

    }


    public async Task<ResultDto<CreatePlatformNFTOutput>> CreatePlatformNFTAsync(CreatePlatformNFTInput input)
    {
        var currentUserAddress = "";
        try
        {
            var createSwitch = _platformOptionsMonitor.CurrentValue.CreateSwitch;
            if (!createSwitch)
            {
                return new ResultDto<CreatePlatformNFTOutput>() {Success = false, Message = "The NFT creation activity has ended"};
            }

            currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress.IsNullOrEmpty())
            {
                return new ResultDto<CreatePlatformNFTOutput>() {Success = false, Message = "Please log out and log in again"};
            }
            _logger.LogInformation("CreatePlatformNFTAsync request currentUserAddress:{A} input:{B}", currentUserAddress, JsonConvert.SerializeObject(input));

            var createLimit = _platformOptionsMonitor.CurrentValue.UserCreateLimit;
            var createPlatformNFTGrain = _clusterClient.GetGrain<ICreatePlatformNFTGrain>(currentUserAddress);
            var grainDto = (await createPlatformNFTGrain.GetCreatePlatformNFTAsync()).Data;
            if (grainDto != null && grainDto.Count >= createLimit)
            {
                return new ResultDto<CreatePlatformNFTOutput>() {Success = false, Message = "You have exceeded the NFT creation limit for this event"};
            }

            var collectionOwnerProxyAccountHash = _platformOptionsMonitor.CurrentValue.CollectionOwnerProxyAccountHash;
            var proxyContractSideChainAddress = _platformOptionsMonitor.CurrentValue.ProxyContractSideChainAddress;
            var privateKey = _platformOptionsMonitor.CurrentValue.PrivateKey;
            var collectionSymbol = _platformOptionsMonitor.CurrentValue.CollectionSymbol;
            var createChainId = _platformOptionsMonitor.CurrentValue.CreateChainId;
            var nftSymbol = "";
            //get current token Id
            var tokenIdGrain = _clusterClient.GetGrain<IPlatformNFTTokenIdGrain>(collectionSymbol);
            var tokenIdGrainDto = (await tokenIdGrain.GetPlatformNFTCurrentTokenIdAsync()).Data;

            if (tokenIdGrainDto == null)
            {
                return new ResultDto<CreatePlatformNFTOutput>() {Success = false, Message = "No token ID information available"};
            }

            var currentTokenId = tokenIdGrainDto.TokenId.IsNullOrEmpty()?"0":tokenIdGrainDto.TokenId;
            var nextTokenId = int.Parse(currentTokenId) + 1;
            nftSymbol = string.Concat(_platformOptionsMonitor.CurrentValue.SymbolPrefix,
                NFTSymbolBasicConstants.NFTSymbolSeparator, nextTokenId);
            
            //create nft
            
            //get transaction result;
            return new ResultDto<CreatePlatformNFTOutput>()
            {
                Success = true, Message = "Success",
                Data = new CreatePlatformNFTOutput()
                {
                    CollectionSymbol = collectionSymbol,
                    CollectionId = string.Concat(createChainId, NFTSymbolBasicConstants.NFTSymbolSeparator, collectionSymbol),
                    NFTSymbol = nftSymbol,
                    NFTId =  string.Concat(createChainId, NFTSymbolBasicConstants.NFTSymbolSeparator, nftSymbol)
                }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreatePlatformNFTAsync Exception address:{A} input:{B} errMsg:{C}",currentUserAddress, JsonConvert.SerializeObject(input), e.Message);
            return new ResultDto<CreatePlatformNFTOutput>() {Success = false, Message = "Service exception"};
        }
    }
}