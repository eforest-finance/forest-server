using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using Forest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.VisualBasic;
using Newtonsoft.Json;
using NFTMarketServer.Ai;
using NFTMarketServer.Ai.Index;
using NFTMarketServer.Basic;
using NFTMarketServer.Common.AElfSdk;
using NFTMarketServer.Common.Http;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Redis;
using NFTMarketServer.Users;
using Org.BouncyCastle.Security;
using Orleans;
using Orleans.Runtime;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DistributedLocking;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Platform;

[RemoteService(IsEnabled = false)]
public class PlatformNFTAppService : NFTMarketServerAppService, IPlatformNFTAppService
{
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly IOptionsMonitor<PlatformNFTOptions> _platformOptionsMonitor;
    private readonly ILogger<PlatformNFTAppService> _logger;
    private readonly IContractProvider _contractProvider;
    private readonly ISymbolIconAppService _symbolIconAppService;
    private readonly IUserAppService _userAppService;
    private readonly INESTRepository<AiCreateIndex, string> _aiCreateIndexRepository;
    private readonly INESTRepository<AIImageIndex, string> _aIImageIndexRepository;
    private readonly IAIArtProvider _aiArtProvider;
    private readonly IHttpService _httpService;
    private readonly IOpenAiRedisTokenBucket _openAiRedisTokenBucket;
    private readonly IOptionsMonitor<AIPromptsOptions> _aiPromptsOptions;
    private readonly IObjectMapper _objectMapper;
    private readonly IAbpDistributedLock _distributedLock;
    private const int PromotMaxLength = 500;
    private const int NegativePromotMaxLength = 400;
    private readonly IClusterClient _clusterClient;


    public PlatformNFTAppService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<PlatformNFTOptions> platformOptionsMonitor,
        ILogger<PlatformNFTAppService> logger,
        IContractProvider contractProvider,
        ISymbolIconAppService symbolIconAppService,
        IUserAppService userAppService,
        INESTRepository<AiCreateIndex, string> aiCreateIndexRepository,
        INESTRepository<AIImageIndex, string> aIImageIndexRepository,
        IAIArtProvider aiArtProvider,
        IHttpService httpService,
        IObjectMapper objectMapper,
        IAbpDistributedLock distributedLock,
        IOpenAiRedisTokenBucket openAiRedisTokenBucket,
        IOptionsMonitor<AIPromptsOptions> aiPromptsOptions,
        IClusterClient clusterClient
    )
    {
        _chainOptionsMonitor = chainOptionsMonitor;
        _platformOptionsMonitor = platformOptionsMonitor;
        _logger = logger;
        _contractProvider = contractProvider;
        _symbolIconAppService = symbolIconAppService;
        _userAppService = userAppService;
        _aiCreateIndexRepository = aiCreateIndexRepository;
        _aIImageIndexRepository = aIImageIndexRepository;
        _aiArtProvider = aiArtProvider;
        _openAiRedisTokenBucket = openAiRedisTokenBucket;
        _aiPromptsOptions = aiPromptsOptions;
        _httpService = httpService;
        _objectMapper = objectMapper;
        _distributedLock = distributedLock;
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

            if (tokenIdGrainDto != null && tokenIdGrainDto.TokenId.IsNullOrEmpty())
            {
                return new ResultDto<CreatePlatformNFTOutput>() {Success = false, Message = "No token ID information available"};
            }

            var currentTokenId = tokenIdGrainDto.TokenId;
            var nextTokenId = currentTokenId + 1;
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