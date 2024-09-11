using System;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Contracts.ProxyAccountContract;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common.AElfSdk;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.NFT;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Dto;
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
    private readonly INFTInfoAppService _nftInfoAppService;


    public PlatformNFTAppService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<PlatformNFTOptions> platformOptionsMonitor,
        ILogger<PlatformNFTAppService> logger,
        IContractProvider contractProvider,
        IUserAppService userAppService,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        INFTInfoAppService nftInfoAppService,
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
        _nftInfoAppService = nftInfoAppService;
    }

    private async Task<string> GenerateRawTransaction(AElfClient client, string methodName, IMessage param,
        string contractAddress, string privateKey)
    {
        return client.SignTransaction(privateKey, await client.GenerateTransactionAsync(
                client.GetAddressFromPrivateKey(privateKey), contractAddress, methodName, param))
            .ToByteArray().ToHex();
    }

    private async Task<TransactionResultDto> CreateAndIssuePlatformNFT(string chainId, string ownerHash, string issuerHash,
        string nftSymbol, string ownerVirtualAddress, string issuerVirtualAddress, int issueChainId, string imageUrl,
        string fileHash, string proxyContrctAddressSide, string privateKey, string userAddress, string tokenName)
    {
        try
        {
            var client = _blockchainClientFactory.GetClient(chainId);
            var createNFTInputParam =
                new BatchCreateTokenInput
                {
                    OwnerProxyAccountHash = Hash.LoadFromHex(ownerHash),
                    IssuerProxyAccountHash = Hash.LoadFromHex(issuerHash),
                    TokenInfos =
                    {
                        new TokenInfo()
                        {
                            Symbol = nftSymbol,
                            TokenName = tokenName,
                            TotalSupply = 10,
                            Decimals = 0,
                            Owner = AElf.Types.Address.FromBase58(ownerVirtualAddress),
                            Issuer = AElf.Types.Address.FromBase58(issuerVirtualAddress),
                            IsBurnable = true,
                            IssueChainId = issueChainId, //1866392,
                            //LockWhiteList = {}
                            ExternalInfo = new ExternalInfo()
                            {
                                Value =
                                {
                                    { "__nft_file_hash", fileHash },
                                    { "__nft_metadata", "[]" },

                                    { "__nft_fileType", "image" },
                                    { "__nft_image_url", imageUrl }
                                }
                            },
                            Amount = 10,
                            To = AElf.Types.Address.FromBase58(userAddress)
                        }
                    }
                };
            var batchCreateTokenRaw =
                await GenerateRawTransaction(client, "BatchCreateToken", createNFTInputParam,
                    proxyContrctAddressSide, privateKey);
            _logger.LogInformation(
                "CreateAndIssuePlatformNFT nftSymbol:{A} imageUrl:{B} userAddress:{C} batchCreateTokenRaw:{D}", nftSymbol,
                imageUrl, userAddress, batchCreateTokenRaw);

            var result = await client.SendTransactionAsync(new SendTransactionInput()
                { RawTransaction = batchCreateTokenRaw });
            await Task.Delay(3000);
            TransactionResultDto transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
            var times = 0;
            while ((transactionResult.Status is "PENDING" or "NOTEXISTED") && times < 60)
            {
                times++;
                await Task.Delay(1000);
                transactionResult = await client.GetTransactionResultAsync(result.TransactionId);
            }
            _logger.LogInformation("CreateAndIssuePlatformNFT nftSymbol:{A} imageUrl:{B} userAddress:{C} transactionResult:{D}",
                nftSymbol, imageUrl, userAddress, JsonConvert.SerializeObject(transactionResult));
            return transactionResult;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateAndIssuePlatformNFT Exception nftSymbol:{A} imageUrl:{B} userAddress:{C} errMsg:{D}",
                nftSymbol, imageUrl, userAddress, e.Message);
        }
        return new TransactionResultDto();
    }

    public async Task<CreatePlatformNFTOutput> CreatePlatformNFTAsync(CreatePlatformNFTInput input)
    {
        _logger.LogInformation("CreatePlatformNFTAsync start input:{A} ", JsonConvert.SerializeObject(input));
        var currentUserAddress = "";
        try
        {
            var createSwitch = _platformOptionsMonitor.CurrentValue.CreateSwitch;
            if (!createSwitch)
            {
                throw new Exception("The NFT creation activity has ended");
            }
            var createPlatformNFTGrain = _clusterClient.GetGrain<ICreatePlatformNFTGrain>(currentUserAddress);
            //update user create record:Prevent duplicate submissions
            await createPlatformNFTGrain.SaveCreatePlatformNFTAsync(new CreatePlatformNFTGrainInput()
            {
                Address = currentUserAddress,
                IsBack = false
            });
            
            var collectionIcon = _platformOptionsMonitor.CurrentValue.CollectionIcon;
            var collectionName = _platformOptionsMonitor.CurrentValue.CollectionName;
            currentUserAddress = await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress.IsNullOrEmpty())
            {
                throw new Exception("Please log out and log in again");
            }

            _logger.LogInformation("CreatePlatformNFTAsync request currentUserAddress:{A} input:{B}",
                currentUserAddress, JsonConvert.SerializeObject(input));

            var createLimit = _platformOptionsMonitor.CurrentValue.UserCreateLimit;
            var grainDto = (await createPlatformNFTGrain.GetCreatePlatformNFTAsync()).Data;
            if (grainDto != null && grainDto.Count > createLimit)
            {
                throw new Exception("You have exceeded the NFT creation limit for this event");
            }

            var collectionSymbol = _platformOptionsMonitor.CurrentValue.CollectionSymbol;
            var nftSymbol = "";
            //get current token Id
            var tokenIdGrain = _clusterClient.GetGrain<IPlatformNFTTokenIdGrain>(collectionSymbol);
            var tokenIdGrainDto = (await tokenIdGrain.GetPlatformNFTCurrentTokenIdAsync()).Data;

            if (tokenIdGrainDto == null)
            {
                throw new Exception("No token ID information available");
            }

            var currentTokenId = tokenIdGrainDto.TokenId.IsNullOrEmpty() ? "0" : tokenIdGrainDto.TokenId;
            var nextTokenId = int.Parse(currentTokenId) + 1;
            //check tokenId 
            var excludeTokenIds = _platformOptionsMonitor.CurrentValue.ExcludeTokenIds;
            while (!excludeTokenIds.IsNullOrEmpty() && excludeTokenIds.Contains(nextTokenId))
            {
                nextTokenId++;
            }
            
            nftSymbol = string.Concat(_platformOptionsMonitor.CurrentValue.SymbolPrefix,
                NFTSymbolBasicConstants.NFTSymbolSeparator, nextTokenId);

            //create nft
            var createChainId = _platformOptionsMonitor.CurrentValue.CreateChainId;
            var issueChainId = _platformOptionsMonitor.CurrentValue.IssueChainId;

            var collectionOwnerProxyAccountHash = _platformOptionsMonitor.CurrentValue.CollectionOwnerProxyAccountHash;
            var collectionIssuerProxyAccountHash =
                _platformOptionsMonitor.CurrentValue.CollectionIssuerProxyAccountHash;
            var collectionOwnerProxyAddress = _platformOptionsMonitor.CurrentValue.CollectionOwnerProxyAddress;
            var collectionIssuerProxyAddress = _platformOptionsMonitor.CurrentValue.CollectionIssuerProxyAddress;
            var proxyContractSideChainAddress = _platformOptionsMonitor.CurrentValue.ProxyContractSideChainAddress;
            var privateKey = _platformOptionsMonitor.CurrentValue.PrivateKey;
            var transactionResultDto = await CreateAndIssuePlatformNFT(
                createChainId,
                collectionOwnerProxyAccountHash,
                collectionIssuerProxyAccountHash,
                nftSymbol,
                collectionOwnerProxyAddress,
                collectionIssuerProxyAddress,
                issueChainId,
                input.NFTUrl,
                input.UrlHash,
                proxyContractSideChainAddress,
                privateKey,
                currentUserAddress,
                input.NFTName);

            //get transaction result;
            if (transactionResultDto == null || transactionResultDto.Status != "MINED")
            {
                _logger.LogError("CreatePlatformNFTAsync Fail address:{A} input:{B} transactionResultDto:{C}", currentUserAddress,
                    JsonConvert.SerializeObject(input), JsonConvert.SerializeObject(transactionResultDto));
                
                if (transactionResultDto.Status == "PENDING" || (!transactionResultDto.Error.IsNullOrEmpty() && transactionResultDto.Error.Contains("Token already exists")))
                {
                    await tokenIdGrain.SavePlatformNFTTokenIdAsync(new PlatformNFTTokenIdGrainInput()
                    {
                        CollectionSymbol = collectionSymbol,
                        TokenId = (nextTokenId).ToString()
                    });
                }
                throw new Exception("chain create nft fail,errMsg:"+transactionResultDto.Error);
            }

            var txId = transactionResultDto.TransactionId;
            //update tokenId
            await tokenIdGrain.SavePlatformNFTTokenIdAsync(new PlatformNFTTokenIdGrainInput()
            {
                CollectionSymbol = collectionSymbol,
                TokenId = nextTokenId.ToString()
            });
            
            // extension write
            await _nftInfoAppService.CreateNFTInfoExtensionAsync(new CreateNFTExtensionInput
            {
                ChainId = createChainId,
                TransactionId = txId,
                Symbol = nftSymbol,
                Description = "",
                ExternalLink = "",
                PreviewImage = input.NFTUrl,
                File = input.NFTUrl,
                CoverImageUrl = input.NFTUrl,
            });
            
            return new CreatePlatformNFTOutput()
            {
                CollectionSymbol = collectionSymbol,
                CollectionId = string.Concat(createChainId, NFTSymbolBasicConstants.NFTSymbolSeparator,
                    collectionSymbol),
                NFTSymbol = nftSymbol,
                NFTId = string.Concat(createChainId, NFTSymbolBasicConstants.NFTSymbolSeparator, nftSymbol),
                CollectionIcon = collectionIcon,
                CollectionName = collectionName,
                TransactionId = txId
            };
        }
        catch (Exception e)
        {
            var createPlatformNFTGrain = _clusterClient.GetGrain<ICreatePlatformNFTGrain>(currentUserAddress);
            await createPlatformNFTGrain.SaveCreatePlatformNFTAsync(new CreatePlatformNFTGrainInput()
            {
                Address = currentUserAddress,
                IsBack = true
            });
            _logger.LogError(e, "CreatePlatformNFTAsync Exception address:{A} input:{B} errMsg:{C}", currentUserAddress,
                JsonConvert.SerializeObject(input), e.Message);
            throw new Exception(e.Message);
        }
    }
    public async Task<CreatePlatformNFTRecordInfo> GetPlatformNFTInfoAsync(string address)
    {
        try
        {
            var createLimit = _platformOptionsMonitor.CurrentValue.UserCreateLimit;
            var createPlatformNFTGrain = _clusterClient.GetGrain<ICreatePlatformNFTGrain>(address);
            var grainDto = (await createPlatformNFTGrain.GetCreatePlatformNFTAsync()).Data;
            var createChainId = _platformOptionsMonitor.CurrentValue.CreateChainId;
            var collectionSymbol = _platformOptionsMonitor.CurrentValue.CollectionSymbol;

            var result = new CreatePlatformNFTRecordInfo()
            {
                NFTCount = 0,
                IsDone = false,
                CollectionId = string.Concat(createChainId, NFTSymbolBasicConstants.NFTSymbolSeparator,collectionSymbol)

            };
            if (grainDto == null || grainDto.Count == 0) return result;

            result.NFTCount = grainDto.Count;
            if (grainDto.Count >= createLimit)
            {
                result.IsDone = true;
            }
            return result;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetPlatformNFTInfoAsync Exception address:{A} errMsg:{C}", address,
                e.Message);
            throw e;
        }
    }
}