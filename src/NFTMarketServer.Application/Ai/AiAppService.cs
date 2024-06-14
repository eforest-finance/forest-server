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
using Newtonsoft.Json;
using NFTMarketServer.Ai.Index;
using NFTMarketServer.Basic;
using NFTMarketServer.Common.AElfSdk;
using NFTMarketServer.Common.Http;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Redis;
using NFTMarketServer.Users;
using Org.BouncyCastle.Security;
using Orleans.Runtime;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DistributedLocking;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Ai;

[RemoteService(IsEnabled = false)]
public class AiAppService : NFTMarketServerAppService, IAiAppService
{
    private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
    private readonly IOptionsMonitor<OpenAiOptions> _openAiOptionsMonitor;
    private readonly ILogger<AiAppService> _logger;
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


    public AiAppService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<OpenAiOptions> openAiOptionsMonitor,
        ILogger<AiAppService> logger,
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
        IOptionsMonitor<AIPromptsOptions> aiPromptsOptions
    )
    {
        _chainOptionsMonitor = chainOptionsMonitor;
        _openAiOptionsMonitor = openAiOptionsMonitor;
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
    }

    public async Task<PagedResultDto<CreateAiArtDto>> CreateAiArtAsync(CreateAiArtInput input)
    {
        var chainId = input.ChainId;
        var transactionId = "";
        var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
        CreateArtInput createArtInput;
        Transaction transaction;
        AiCreateIndex aiCreateIndex;
        try
        {
            transaction = TransferHelper.TransferToTransaction(input.RawTransaction);
            createArtInput = TransferToCreateArtInput(transaction, chainId);
            
            transactionId = await SendTransactionAsync(chainId, transaction);
            _logger.LogInformation("CreateAiArtAsync chainId={A} transactionId={B} fromAddress={C} createArtInput={D}",
                chainId,
                transactionId, transaction.From.ToBase58(),
                JsonConvert.SerializeObject(createArtInput));
            aiCreateIndex = BuildAiCreateIndex(transactionId, transaction.From.ToBase58(), createArtInput,
                currentUserAddress);
            await _aiCreateIndexRepository.AddAsync(aiCreateIndex);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendTransactionAsync error transactionId={A} request={B} rawTransaction={C}",
                transactionId, JsonConvert.SerializeObject(input), input?.RawTransaction);
            throw new SystemException(e.Message);
        }

        var s3UrlDic = await GenerateImageAsync(transaction.From.ToBase58(), transactionId,
            aiCreateIndex, currentUserAddress);

        return new PagedResultDto<CreateAiArtDto>()
        {
            TotalCount = s3UrlDic.Count,
            Items = s3UrlDic
                .Select(kvp => new CreateAiArtDto { Url = kvp.Key, Hash = kvp.Value.Replace("\"","") })
                .ToList()
        };
    }
    public async Task<CreateAiResultDto> CreateAiArtAsyncV2(CreateAiArtInput input)
    {
        var chainId = input.ChainId;
        string transactionId = null;
        var isCanRetry = false;
        var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
        CreateArtInput createArtInput;
        Transaction transaction;
        AiCreateIndex aiCreateIndex;
        Dictionary<string, string> s3UrlDic;
        try
        {
            transaction = TransferHelper.TransferToTransaction(input.RawTransaction);
            createArtInput = TransferToCreateArtInput(transaction, chainId);
            //Sensitive words check
            var wordCheckRes = await SensitiveWordCheckAsync(createArtInput.Promt, createArtInput.NegativePrompt);
            if (!wordCheckRes.Success)
            {
                return new CreateAiResultDto()
                {
                    CanRetry = isCanRetry,
                    TransactionId = transactionId,
                    Success = false, ErrorMsg = wordCheckRes.Message
                };
            }
            //send transaction
            transactionId = await SendTransactionAsync(chainId, transaction);
            _logger.LogInformation("CreateAiArtAsyncV2 chainId={A} transactionId={B} fromAddress={C} createArtInput={D}",
                chainId,
                transactionId, transaction.From.ToBase58(),
                JsonConvert.SerializeObject(createArtInput));
            if (!transactionId.IsNullOrEmpty())
            {
                isCanRetry = true;
            }
            //add record
            aiCreateIndex = BuildAiCreateIndex(transactionId, transaction.From.ToBase58(), createArtInput,
                currentUserAddress);
            await _aiCreateIndexRepository.AddAsync(aiCreateIndex);
            //create image
            s3UrlDic = await GenerateImageAsync(transaction.From.ToBase58(), transactionId,
                aiCreateIndex, currentUserAddress);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "CreateAiArtAsyncV2 error transactionId={A} request={B} rawTransaction={C}",
                transactionId, JsonConvert.SerializeObject(input), input?.RawTransaction);

            return new CreateAiResultDto()
            {
                CanRetry = isCanRetry,
                TransactionId = transactionId,
                Success = false, ErrorMsg = e.Message,
            };
        }

        return new CreateAiResultDto()
        {
            Success = true, ErrorMsg = "",
            CanRetry = false,
            TransactionId = transactionId,
            TotalCount = s3UrlDic.Count,
            itms = s3UrlDic
                .Select(kvp => new CreateAiArtDto { Url = kvp.Key, Hash = kvp.Value.Replace("\"", "") })
                .ToList()
        };
    }

    private async Task<ResultDto<string>> SensitiveWordCheckAsync(string promot, string negativePromot)
    {
        if (promot.Length > PromotMaxLength)
        {
            var message = $"Prompt words with a length exceeding {PromotMaxLength}";
            return new ResultDto<string>()
            {
                Success = false, Message = message
            };
        }
        if (negativePromot.Length > NegativePromotMaxLength)
        {
            var message = $"NegativePrompt words with a length exceeding {NegativePromotMaxLength}";
            return new ResultDto<string>()
            {
                Success = false, Message = message
            };
        }
        
        var openAiUrl = _openAiOptionsMonitor.CurrentValue.WordCheckUrl;
        var openAiRequestBody = JsonConvert.SerializeObject(new OpenAiWordCheckDto
        {
            Input = String.Concat(promot, " ", negativePromot)
        });
       
        var openAiHeader = new Dictionary<string, string>
        {
            //[CommonConstant.Authorization] = CommonConstant.BearerToken + _openAiOptionsMonitor.CurrentValue.ApiKeyList.First()
            [CommonConstant.Authorization] = CommonConstant.BearerToken + _openAiOptionsMonitor.CurrentValue.ApiKeyListTmp.First()

        };

        var result = new OpenAiWordCheckResponse();
        try
        {
            var openAiResult =
                await _httpService.SendPostRequest(openAiUrl, openAiRequestBody, openAiHeader, CommonConstant.IntOne);
            result = JsonConvert.DeserializeObject<OpenAiWordCheckResponse>(openAiResult);
        }
        catch (Exception e)
        {
            _logger.LogError(e,
                "SensitiveWordCheckAsync Promt {A} NegativePrompt={B}", promot, negativePromot);
            throw new SystemException($"Network error, please try again. {e.Message}");
        }

        if (result == null || result.Results == null)
        {
            throw new SystemException("Network error, please try again");
        }

        if (!result.Results.First().Flagged)
        {
            return new ResultDto<string>()
            {
                Success = true, Message = ""
            };
        }

        {
            var message = new StringBuilder();
            message.Append("Your promot contains sensitive words, type:");
            var category = result.Results.First().Categories;
            var flag = false;
            if (category.TryGetValue("sexual", out flag))  
            {
                if (flag)
                {
                    message.Append("Sexual ");
                    flag = false;
                }
            } 
            if (category.TryGetValue("hate", out flag))  
            {
                if (flag)
                {
                    message.Append("hate ");
                    flag = false;
                }
            } 
            
            if (category.TryGetValue("harassment", out flag))  
            {
                if (flag)
                {
                    message.Append("harassment ");
                    flag = false;
                }
            }
            if (category.TryGetValue("self-harm", out flag))  
            {
                if (flag)
                {
                    message.Append("self-harm ");
                    flag = false;
                }
            }
            if (category.TryGetValue("sexual/minors", out flag))  
            {
                if (flag)
                {
                    message.Append("sexual/minors ");
                    flag = false;
                }
            }
            if (category.TryGetValue("hate/threatening", out flag))  
            {
                if (flag)
                {
                    message.Append("hate/threatening ");
                    flag = false;
                }
            }
            if (category.TryGetValue("violence/graphic", out flag))  
            {
                if (flag)
                {
                    message.Append("violence/graphic ");
                    flag = false;
                }
            }
            if (category.TryGetValue("self-harm/intent", out flag))  
            {
                if (flag)
                {
                    message.Append("self-harm/intent ");
                    flag = false;
                }
            }
            if (category.TryGetValue("self-harm/instructions", out flag))  
            {
                if (flag)
                {
                    message.Append("self-harm/instructions ");
                    flag = false;
                }
            }
            if (category.TryGetValue("harassment/threatening", out flag))  
            {
                if (flag)
                {
                    message.Append("harassment/threatening ");
                    flag = false;
                }
            }
            if (category.TryGetValue("violence", out flag))  
            {
                if (flag)
                {
                    message.Append("violence ");
                    flag = false;
                }
            }

            return new ResultDto<string>()
            {
                Success = false, Message = message.ToString()
            };
        }

    }

    private static AiCreateIndex BuildAiCreateIndex(string transactionId, string from, CreateArtInput createArtInput,
        string currentUserAddress)
    {
        if (createArtInput.Number < CommonConstant.IntOne || createArtInput.Number > CommonConstant.IntTen)
        {
            throw new ArgumentException("The number per transaction needs to be between 1 and 10.");
        }
        return new AiCreateIndex
        {
            Id = IdGenerateHelper.GetAiCreateId(transactionId, from),
            TransactionId = transactionId,
            Address = currentUserAddress,
            Ctime = DateTime.UtcNow,
            Utime = DateTime.UtcNow,
            Model = createArtInput.Model.ToEnum<AiModelType>(),
            NegativePrompt = createArtInput.NegativePrompt,
            Number = createArtInput.Number,
            PaintingStyle = createArtInput.PaintingStyle.ToEnum<AiPaintingStyleType>(),
            Promt = createArtInput.Promt,
            Size = createArtInput.Size.ToEnum<AiSizeType>(),
            Status = AiCreateStatus.PAYSUCCESS,
            RetryCount = 0
        };
    }
    
    private CreateArtInput TransferToCreateArtInput(Transaction transaction,string chainId)
    {
        var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainId];
        var forestContractAddress = chainInfo?.ForestContractAddress;
        var caContractAddress = chainInfo?.CaContractAddress;
        _logger.Debug("forestContractAddress = {A}, transaction.To = {B}, transaction.MethodName = {C}",
            chainInfo?.ForestContractAddress, transaction.To, transaction.MethodName);

        CreateArtInput createArtInput;
        if (transaction.To.ToBase58().Equals(caContractAddress) &&
            transaction.MethodName == CommonConstant.MethodManagerForwardCall)
        {
            var managerForwardCallInput = ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
            if (managerForwardCallInput.MethodName == CommonConstant.MethodCreateArt)
            {
                createArtInput = CreateArtInput.Parser.ParseFrom(managerForwardCallInput.Args);
            }
            else
            {
                throw new UserFriendlyException("Invalid transaction");
            }
        }
        else if (transaction.To.ToBase58().Equals(forestContractAddress) && transaction.MethodName == CommonConstant.MethodCreateArt)
        {
            createArtInput = CreateArtInput.Parser.ParseFrom(transaction.Params);
        }
        else
        {
            throw new UserFriendlyException("Invalid transaction");
        }

        return createArtInput;
    }
    private async Task<Dictionary<string,string>> GenerateImageAsync(string fromAddress,
        string transactionId, AiCreateIndex aiCreateIndex, string currentUserAddress)
    {
        var openAiUrl = _openAiOptionsMonitor.CurrentValue.ImagesUrlV1;
        var openAiRequestBody = JsonConvert.SerializeObject(new OpenAiImageGenerationDto
        {
            Model = aiCreateIndex.Model.ToEnumString(),
            N = aiCreateIndex.Number,
            Size = aiCreateIndex.Size.ToEnumString(),
            Prompt = BuildFullPrompt(aiCreateIndex)
        });
       
        var openAiHeader = new Dictionary<string, string>
        {
            [CommonConstant.Authorization] = CommonConstant.BearerToken + _openAiOptionsMonitor.CurrentValue.ApiKeyList[_openAiRedisTokenBucket.GetNextToken()]
        };

        var result = new OpenAiImageGenerationResponse();
        var retryCount = CommonConstant.IntOne;
        var openAiMsg = "";
        for (; retryCount <= CommonConstant.IntThree; retryCount++)
        {
            var openAiResult = "";
            try
            {
                openAiResult =
                    await _httpService.SendPostRequest(openAiUrl, openAiRequestBody, openAiHeader, CommonConstant.IntOne);
                result = JsonConvert.DeserializeObject<OpenAiImageGenerationResponse>(openAiResult);
            }
            catch (Exception e)
            {
                openAiMsg = "Url=" + openAiUrl + " .openAiRequestBody=" + openAiRequestBody + " .aiCreateIndex=" +
                            JsonConvert.SerializeObject(aiCreateIndex) + e.Message;
                _logger.LogError(e,
                    "OpenAiImageGeneration Error {A} retryCount={B}", openAiMsg, retryCount);
            }

            if (result != null && result.Data?.Count > 0) break;
        }

        aiCreateIndex.RetryCount += retryCount;
        if (result == null || result.Data.IsNullOrEmpty())
        {
            aiCreateIndex.Result = openAiMsg.IsNullOrEmpty() ? (result?.Error?.Message) : openAiMsg;
            aiCreateIndex.Utime = DateTime.UtcNow;
            await _aiCreateIndexRepository.UpdateAsync(aiCreateIndex);
            _logger.LogError("Ai Image Generation Error {A} transactionId={B} fromAddress={C}",
                JsonConvert.SerializeObject(aiCreateIndex.Result), transactionId, fromAddress);
            throw new SystemException("Ai Image Generation Error. " + result?.Error?.Message);
        }
        else
        {
            aiCreateIndex.Status = AiCreateStatus.IMAGECREATED;
            aiCreateIndex.Utime = DateTime.UtcNow;
            await _aiCreateIndexRepository.UpdateAsync(aiCreateIndex);
        }
        
        
        var s3UrlDic = new Dictionary<string,string>();
        var addList = new List<AIImageIndex>();
        for (var j = 0; j < result.Data.Count; j++)
        {
            var openAiImageGeneration = result.Data[j];
            
            try
            {
                var imageBytes =
                    await _httpService.DownloadImageAsUtf8BytesAsync(openAiImageGeneration.Url, CommonConstant.IntThree);
                var s3UrlValuePairs = await _symbolIconAppService.UpdateNFTIconWithHashAsync(imageBytes,
                    transactionId + CommonConstant.Underscore + fromAddress + CommonConstant.Underscore +
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + CommonConstant.ImagePNG);
                if (s3UrlValuePairs.Key.IsNullOrEmpty()) continue;
                s3UrlDic.Add(s3UrlValuePairs.Key,s3UrlValuePairs.Value);
                addList.Add(new AIImageIndex
                {
                    Id = IdGenerateHelper.GetAIImageId(transactionId, fromAddress, j),
                    Address = currentUserAddress,
                    Ctime = DateTime.UtcNow,
                    Utime = DateTime.UtcNow,
                    S3Url = s3UrlValuePairs.Key,
                    Hash = s3UrlValuePairs.Value.Replace("\"",""),
                    TransactionId = transactionId
                });
                
            }
            catch (Exception e)
            {
                aiCreateIndex.Result = e.Message;
                aiCreateIndex.Utime = DateTime.UtcNow;
                await _aiCreateIndexRepository.UpdateAsync(aiCreateIndex);
                _logger.LogError(e, "s3 upload error openAiImageGeneration={A}", openAiImageGeneration.Url);
                throw new SystemException("s3 upload error", e);
            }
        }
        
        await _aIImageIndexRepository.BulkAddOrUpdateAsync(addList);
        aiCreateIndex.Status = AiCreateStatus.UPLOADS3;
        aiCreateIndex.Utime = DateTime.UtcNow;
        await _aiCreateIndexRepository.UpdateAsync(aiCreateIndex);
        return s3UrlDic;
    }

    private static string BuildFullPrompt(AiCreateIndex aiCreateIndex)
    {
        return "prompt：" + aiCreateIndex.Promt + ";" + (aiCreateIndex.PaintingStyle == null
                   ? ""
                   : "; Image style:" + aiCreateIndex.PaintingStyle.ToEnumString())
               + (aiCreateIndex.NegativePrompt.IsNullOrEmpty()
                   ? ""
                   : "; negative prompt:" + aiCreateIndex.NegativePrompt);
    }

    private async Task<string> SendTransactionAsync(string chainId, Transaction transaction)
    {
        var transactionOutput = await _contractProvider.SendTransactionAsync(chainId, transaction);

        var transactionId = transactionOutput.TransactionId;

        if (!_openAiOptionsMonitor.CurrentValue.RepeatRequestIsOn)
        {
            var id = IdGenerateHelper.GetAiCreateId(transactionId, transaction.From.ToBase58());
            var existRecord = await _aiCreateIndexRepository.GetAsync(id);
            if (existRecord != null)
            {
                throw new ArgumentException("Please do not initiate duplicate requests.");
            }
        }

        var transactionResultDto = await _contractProvider.QueryTransactionResult(transactionId, chainId);
        if (transactionResultDto == null)
        {
            _logger.LogError("QueryTransactionResult is null transactionId={A}", transactionId);
            throw new SystemException("QueryTransactionResult is null transactionId=" + transactionId);
        }

        while (transactionResultDto.Status.Equals(TransactionState.Pending))
        {
            await Task.Delay(CommonConstant.IntOneThousand);
            transactionResultDto = await _contractProvider.QueryTransactionResult(transactionId, chainId);
        }
        
        for (var i = 0;
             (transactionResultDto.Status.Equals(TransactionState.Notexisted) ||
              transactionResultDto.Status.Equals(TransactionState.Pending)) &&
             i <= _openAiOptionsMonitor.CurrentValue.DelayMaxTime;
             i++)
        {
            await Task.Delay(_openAiOptionsMonitor.CurrentValue.DelayMillisecond);
            transactionResultDto = await _contractProvider.QueryTransactionResult(transactionId, chainId);
            while (transactionResultDto.Status.Equals(TransactionState.Pending))
            {
                await Task.Delay(CommonConstant.IntOneThousand);
                transactionResultDto = await _contractProvider.QueryTransactionResult(transactionId, chainId);
            }
        }
        
        if (!transactionResultDto.Status.Equals(TransactionState.Mined))
        {
            _logger.LogError("QueryTransactionResult is fail, transactionId={A} result={B} status={C}", transactionId,
                JsonConvert.SerializeObject(transactionResultDto), transactionResultDto.Status);
            throw new SystemException("QueryTransactionResult is fail transactionId=" + transactionId + "status=" +
                                      transactionResultDto.Status);
        }

        return transactionId;
    }
    public async Task<PagedResultDto<CreateAiArtDto>> GetAiArtsAsync(GetAIArtsInput input)
    {
        try
        {
            if (input.Address.IsNullOrEmpty())
            {
                input.Address = await _userAppService.GetCurrentUserAddressAsync();
            }
            if (input.Address.IsNullOrEmpty())
            {
                _logger.LogError("invalid address");
                throw new UserFriendlyException("invalid address.");
            }
            var tuple = await _aiArtProvider.GetAIImageListAsync(new SearchAIArtsInput()
            {
                Address = input.Address,
                SkipCount = input.SkipCount,
                MaxResultCount = input.MaxResultCount,
                Status = input.Status
            });

            if (tuple == null || tuple.Item1 == 0)
            {
                return new PagedResultDto<CreateAiArtDto>();
            }

            var artList = tuple.Item2;
            return new PagedResultDto<CreateAiArtDto>()
            {
                TotalCount = tuple.Item1,
                Items = artList.Select(x => new CreateAiArtDto  
                {  
                    Url = x.S3Url,
                    Hash = x.Hash  
                }).ToList()
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e,"GetAiArtsAsync Exception: user:{address}",input.Address);
            throw new UserFriendlyException("GetAiArtsAsync Exception: user:{address} e:{error}",input.Address, e.Message);
        }
    }

    public async Task<ResultDto<string>> UseAIArtsAsync(UseAIArtsInput input)
    {
        var currentAddress = "";
        try
        {
            if (input.ImageList.IsNullOrEmpty())
            {
                _logger.LogError("invalid imageIds");
                throw new UserFriendlyException("invalid imageIds");
            }
            currentAddress =  await _userAppService.GetCurrentUserAddressAsync();
            _logger.LogInformation("UseAIArtsAsync request, address:{address}, input:{input}",currentAddress,JsonConvert.SerializeObject(input));
            if (currentAddress.IsNullOrEmpty())
            {
                _logger.LogError("please login");
                throw new UserFriendlyException("please login");
            }

            var status = (int)AiImageUseStatus.USE;
            if (input.Status.Equals((int)AiImageUseStatus.ABANDONED))
            {
                status = (int)AiImageUseStatus.ABANDONED;
            }

            var tuple = await _aiArtProvider.GetAIImageListAsync(new SearchAIArtsInput()
            {
                Address = currentAddress,
                SkipCount = 0,
                MaxResultCount = input.ImageList.Count,
                Status =  (int)AiImageUseStatus.UNUSE,
                ImageHash = input.ImageList,
            });
            
            if (tuple == null || tuple.Item1 == 0)
            {
                _logger.LogInformation("UseAIArtsAsync Image not found, address:{address}, input:{input}",currentAddress,JsonConvert.SerializeObject(input));
                var result = new ResultDto<string>()
                {
                    Success = false,
                    Message = "Please enter your correct imageId"
                };
                return result;
            }

            var images = tuple.Item2;
            foreach (var imageIndex in images)
            {
                imageIndex.status = status;
            }
            var artList = tuple.Item2;
            await _aIImageIndexRepository.BulkAddOrUpdateAsync(images);
        }
        catch (Exception e)
        {
            _logger.LogError(e,"UseAIArtsAsync Exception address:{address}, input:{input}",currentAddress,JsonConvert.SerializeObject(input));
            return new ResultDto<string>() {Success = false, Message = e.Message };

        }
        return new ResultDto<string>() {Success = true, Message = "" };
    }

    public ResultDto<string> GETAIPrompts()
    {
        var promptConfig = _aiPromptsOptions?.CurrentValue;
        if (promptConfig == null || promptConfig.AIPrompts.IsNullOrEmpty())
        {
            return new ResultDto<string>() {Success = false, Message = "have not config prompts " };
        }
        var words = promptConfig.AIPrompts.Split('/');
        var random = new Random();
        var randomWords = words.OrderBy(x => random.Next()).Take(20).ToArray();
        var result = string.Join(" ", randomWords);

        return new ResultDto<string>() {Success = true, Message = "", Data = result};

    }

    public async Task<PagedResultDto<CreateAiArtDto>> CreateAiArtRetryAsync(CreateAiArtRetryInput input)
    {
        if (input == null || input.TransactionId.IsNullOrEmpty())
        {
            throw new InvalidParameterException("The request parameter must not be null.");
        }
        var address = await _userAppService.GetCurrentUserAddressAsync();
        var lockName = CommonConstant.CreateAiArtRetryLockPrefix + input.TransactionId +
                       address;
        await using var lockHandle = await _distributedLock.TryAcquireAsync(lockName);
        if (lockHandle == null)
        {
            _logger.LogError(
                "CreateAiArtRetryAsync Another request is running. Please do not initiate duplicate requests. TransactionId={A} address={B}",
                input.TransactionId, address);
            throw new SystemException("Another request is running. Please do not initiate duplicate requests");
        }

        try
        {
            await Task.Delay(CommonConstant.IntOneThousand);

            var aiCreateIndex =
                await _aiArtProvider.GetAiCreateIndexByTransactionId(input.TransactionId, address);
            if (aiCreateIndex == null)
            {
                _logger.LogError(
                    "CreateAiArtRetryAsync The request parameter does not exist. TransactionId={A} address={B}",
                    input.TransactionId, address);
                throw new InvalidParameterException("The request parameter does not exist. TransactionId=" +
                                                    input.TransactionId);
            }

            if (aiCreateIndex.Status == AiCreateStatus.UPLOADS3)
            {
                _logger.LogError(
                    "CreateAiArtRetryAsync Request has succeeded. Please do not initiate duplicate requests. TransactionId={A} address={B}",
                    input.TransactionId, address);
                throw new InvalidParameterException(
                    "Request has succeeded. Please do not initiate duplicate requests.");
            }

            var s3UrlDic = await GenerateImageAsync(address, input.TransactionId,
                aiCreateIndex, address);

            return new PagedResultDto<CreateAiArtDto>()
            {
                TotalCount = s3UrlDic.Count,
                Items = s3UrlDic
                    .Select(kvp => new CreateAiArtDto { Url = kvp.Key, Hash = kvp.Value.Replace("\"", "") })
                    .ToList()
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "CreateAiArtRetryAsync something is wrong. TransactionId={A} address={B}",
                input.TransactionId, address);
            throw new SystemException("Something is wrong : "+ex.Message);
        }
        finally
        {
            _logger.LogInformation("CreateAiArtRetryAsync Lock released. lockName = {A}",lockName);
        }
    }

    public async Task<PagedResultDto<AiArtFailDto>> QueryAiArtFailAsync(QueryAiArtFailInput input)
    {
        var address = await _userAppService.TryGetCurrentUserAddressAsync();
        if (address.IsNullOrEmpty())
        {
            return new PagedResultDto<AiArtFailDto>()
            {
                TotalCount = CommonConstant.IntZero,
                Items = new List<AiArtFailDto>()
            };
        }

        var result = await _aiArtProvider.GetFailAiCreateIndexListAsync(address, input);
        if (result == null || result.Item1 <= CommonConstant.IntZero)
        {
            return new PagedResultDto<AiArtFailDto>()
            {
                TotalCount = CommonConstant.IntZero,
                Items = new List<AiArtFailDto>()
            };
        }
        return new PagedResultDto<AiArtFailDto>()
        {
            TotalCount = result.Item1,
            Items = _objectMapper.Map<List<AiCreateIndex>, List<AiArtFailDto>>(result.Item2)
        };
    }
}