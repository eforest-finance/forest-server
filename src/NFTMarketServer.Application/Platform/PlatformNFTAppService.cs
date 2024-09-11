using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Proto;
using AElf.Client.Service;
using AElf.Contracts.ProxyAccountContract;
using Forest;
using Google.Protobuf;
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
using Hash = AElf.Types.Hash;

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
    private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;



    public PlatformNFTAppService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<PlatformNFTOptions> platformOptionsMonitor,
        ILogger<PlatformNFTAppService> logger,
        IContractProvider contractProvider,
        IUserAppService userAppService,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        IBlockchainClientFactory<AElfClient> blockchainClientFactory
    )
    {
        _chainOptionsMonitor = chainOptionsMonitor;
        _platformOptionsMonitor = platformOptionsMonitor;
        _logger = logger;
        _contractProvider = contractProvider;
        _userAppService = userAppService;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _blockchainClientFactory = blockchainClientFactory;

    }
    public async Task SendSetCollectionListTotalCountTxAsync(string address, string symbol, long count, string chainId)
    {
        /*var setCollectionListTotalCountParam =
            new SetCollectionListTotalCountInput
            {
                Symbol = symbol,
                Address = Address.FromBase58(address),
                Count = count
            };
        var client = _blockchainClientFactory.GetClient(chainId);
        var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainId];

        var setCollectionListTotalCountParamRaw =
            await GenerateRawTransaction(client, "SetCollectionListTotalCount", setCollectionListTotalCountParam,
                chainInfo.ForestContractAddress, chainInfo.PrivateKey);
            
        var validateTokenInfoExistsResult = await client.SendTransactionAsync(new SendTransactionInput()
            { RawTransaction = setCollectionListTotalCountParamRaw });
        _logger.LogInformation("StatisticsUserListRecord Step5 send tx address:{A} count:{B} symbol:{C} txId:{D}", address, count, symbol, validateTokenInfoExistsResult.TransactionId);
        */

    }
        
    private async Task<string> GenerateRawTransaction(AElfClient client, string methodName, IMessage param,
        string contractAddress, string privateKey)
    {
        return client.SignTransaction(privateKey, await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(privateKey), contractAddress, methodName, param))
            .ToByteArray().ToHex();
    }
    private void CreateNFT(string chainId, string ownerHash, string issuerHash, string nftSymbol, string ownerVirtualAddress, string issuerVirtualAddress)
    {
        var client = _blockchainClientFactory.GetClient(chainId);
        var createNFTInput =
            new BatchCreateTokenInput
            {
                OwnerProxyAccountHash = Hash.LoadFromHex(ownerHash),
                IssuerProxyAccountHash = Hash.LoadFromHex(issuerHash),
                TokenInfos =
                {
                    new TokenInfo()
                    {
                        Symbol = nftSymbol,
                        TokenName = nftSymbol,
                        TotalSupply = 10,
                        Decimals = 8,
                        Owner = AElf.Types.Address.FromBase58(ownerVirtualAddress),
                        Issuer = AElf.Types.Address.FromBase58(ownerVirtualAddress),
                    }
                }
            };
    }

    private void IssueNFT()
    {
    }

    public async Task<CreatePlatformNFTOutput> CreatePlatformNFTAsync(CreatePlatformNFTInput input)
    {
        var currentUserAddress = "";
        try
        {
            
            var createSwitch = _platformOptionsMonitor.CurrentValue.CreateSwitch;
            if (!createSwitch)
            {
                throw new Exception("The NFT creation activity has ended");
            }

            currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress.IsNullOrEmpty())
            {
                throw new Exception("Please log out and log in again");
            }
            _logger.LogInformation("CreatePlatformNFTAsync request currentUserAddress:{A} input:{B}", currentUserAddress, JsonConvert.SerializeObject(input));

            var createLimit = _platformOptionsMonitor.CurrentValue.UserCreateLimit;
            var createPlatformNFTGrain = _clusterClient.GetGrain<ICreatePlatformNFTGrain>(currentUserAddress);
            var grainDto = (await createPlatformNFTGrain.GetCreatePlatformNFTAsync()).Data;
            if (grainDto != null && grainDto.Count >= createLimit)
            {
                throw new Exception("You have exceeded the NFT creation limit for this event");
            }

            var collectionOwnerProxyAccountHash = _platformOptionsMonitor.CurrentValue.CollectionOwnerProxyAccountHash;
            var collectionIssuerProxyAccountHash = _platformOptionsMonitor.CurrentValue.CollectionIssuerProxyAccountHash;
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
                throw new Exception("No token ID information available");
            }

            var currentTokenId = tokenIdGrainDto.TokenId.IsNullOrEmpty()?"0":tokenIdGrainDto.TokenId;
            var nextTokenId = int.Parse(currentTokenId) + 1;
            nftSymbol = string.Concat(_platformOptionsMonitor.CurrentValue.SymbolPrefix,
                NFTSymbolBasicConstants.NFTSymbolSeparator, nextTokenId);
            
            //create nft
            
            
            //get transaction result;
            return new CreatePlatformNFTOutput()
            {
                CollectionSymbol = collectionSymbol,
                CollectionId = string.Concat(createChainId, NFTSymbolBasicConstants.NFTSymbolSeparator,collectionSymbol),
                NFTSymbol = nftSymbol,
                NFTId = string.Concat(createChainId, NFTSymbolBasicConstants.NFTSymbolSeparator, nftSymbol),
                CollectionIcon = ""
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreatePlatformNFTAsync Exception address:{A} input:{B} errMsg:{C}",currentUserAddress, JsonConvert.SerializeObject(input), e.Message);
            throw new Exception("Service exception");
        }
    }
}