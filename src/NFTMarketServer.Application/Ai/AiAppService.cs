using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using Forest;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
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
using Orleans.Runtime;
using Portkey.Contracts.CA;
using Volo.Abp;
using Volo.Abp.Application.Dtos;

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

    }

    public async Task<PagedResultDto<CreateAiArtDto>> CreateAiArtAsync(CreateAiArtInput input)
    {
        var chainId = input.ChainId;
        string transactionId;
        var currentUserAddress =  await _userAppService.GetCurrentUserAddressAsync();
        CreateArtInput createArtInput;
        Transaction transaction;
        AiCreateIndex aiCreateIndex;
        try
        {
            transaction = TransferHelper.TransferToTransaction(input.RawTransaction);
            createArtInput = TransferToCreateArtInput(transaction, chainId);
            transactionId = await SendTransactionAsync(chainId, transaction);
            aiCreateIndex = BuildAiCreateIndex(transactionId, transaction, createArtInput, currentUserAddress);
            await _aiCreateIndexRepository.AddAsync(aiCreateIndex);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendTransactionAsync error");
            throw new SystemException(e.Message);
        }

        var s3UrlDic = await GenerateImageAsync(createArtInput, transaction, transactionId, aiCreateIndex, currentUserAddress);

        return new PagedResultDto<CreateAiArtDto>()
        {
            TotalCount = s3UrlDic.Count,
            Items = s3UrlDic
                .Select(kvp => new CreateAiArtDto { Url = kvp.Key, Hash = kvp.Value.Replace("\"","") })
                .ToList()
        };
    }
    
    private static AiCreateIndex BuildAiCreateIndex(string transactionId,Transaction transaction,CreateArtInput createArtInput, string currentUserAddress)
    {
        if (createArtInput.Number < CommonConstant.IntOne || createArtInput.Number > CommonConstant.IntTen)
        {
            throw new ArgumentException("The number per transaction needs to be between 1 and 10.");
        }
        return new AiCreateIndex
        {
            Id = IdGenerateHelper.GetAiCreateId(transactionId, transaction.From.ToBase58()),
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
    private async Task<Dictionary<string,string>> GenerateImageAsync(CreateArtInput createArtInput, Transaction transaction,
        string transactionId, AiCreateIndex aiCreateIndex, string currentUserAddress)
    {
        var openAiUrl = _openAiOptionsMonitor.CurrentValue.ImagesUrlV1;
        var openAiRequestBody = JsonConvert.SerializeObject(new OpenAiImageGenerationDto
        {
            Model = createArtInput.Model,
            N = createArtInput.Number,
            Size = createArtInput.Size,
            Prompt = BuildFullPrompt(createArtInput)
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
            var openAiResult =
                await _httpService.SendPostRequest(openAiUrl, openAiRequestBody, openAiHeader, CommonConstant.IntOne);
            try
            {
                result = JsonConvert.DeserializeObject<OpenAiImageGenerationResponse>(openAiResult);
            }
            catch (Exception e)
            {
               openAiMsg = "OpenAiImageGeneration Error Url=" + openAiUrl + " openAiRequestBody=" + openAiRequestBody
                           + " result=" + openAiResult + " createArtInput=" +
                           JsonConvert.SerializeObject(createArtInput);
                _logger.LogError(e,
                    "OpenAiImageGeneration Error Url={A} openAiRequestBody={B} result={C} createArtInput={D}",
                    openAiUrl,
                    openAiRequestBody, openAiResult, JsonConvert.SerializeObject(createArtInput));
            }

            if (result != null && result.Data?.Count > 0) break;
        }

        aiCreateIndex.RetryCount = retryCount;
        if (result == null || result.Data.IsNullOrEmpty())
        {
            aiCreateIndex.Result = openAiMsg;
            await _aiCreateIndexRepository.UpdateAsync(aiCreateIndex);
            _logger.LogError("Ai Image Generation Error {A}",JsonConvert.SerializeObject(result));
            throw new SystemException("Ai Image Generation Error. "+result?.Error.Message);
        }
        else
        {
            aiCreateIndex.Status = AiCreateStatus.IMAGECREATED;
            await _aiCreateIndexRepository.UpdateAsync(aiCreateIndex);
        }
        
        
        var s3UrlDic = new Dictionary<string,string>();
        var addList = new List<AIImageIndex>();
        for (var j = 0; j < result.Data.Count; j++)
        {
            var openAiImageGeneration = result.Data[j];
            var imageBytes = await _httpService.DownloadImageAsUtf8BytesAsync(openAiImageGeneration.Url);

            try
            {
                var s3UrlValuePairs = await _symbolIconAppService.UpdateNFTIconWithHashAsync(imageBytes,
                    transactionId + CommonConstant.Underscore + transaction.From.ToBase58() + CommonConstant.Underscore +
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + CommonConstant.ImagePNG);
                if (s3UrlValuePairs.Key.IsNullOrEmpty()) continue;
                s3UrlDic.Add(s3UrlValuePairs.Key,s3UrlValuePairs.Value);
                addList.Add(new AIImageIndex
                {
                    Id = IdGenerateHelper.GetAIImageId(transactionId, transaction.To.ToBase58(), j),
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
                _logger.LogError("s3 upload error openAiImageGeneration={A}", openAiImageGeneration.Url);
            }
        }

        if (addList.IsNullOrEmpty())
        {
            throw new SystemException("s3 upload error,result is empty");
        }
        await _aIImageIndexRepository.BulkAddOrUpdateAsync(addList);
        return s3UrlDic;
    }

    private static string BuildFullPrompt(CreateArtInput createArtInput)
    {
        return "prompt：" + createArtInput.Promt + ";" + (createArtInput.PaintingStyle.IsNullOrEmpty()
                   ? ""
                   : "; Image style:" + createArtInput.PaintingStyle)
               + (createArtInput.NegativePrompt.IsNullOrEmpty()
                   ? ""
                   : "; negative prompt:" + createArtInput.NegativePrompt);
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
            transactionResultDto = await _contractProvider.QueryTransactionResult(transactionId, chainId);
        }

        
        for (var i = 0; transactionResultDto.Status.Equals(TransactionState.Notexisted) && i <= _openAiOptionsMonitor.CurrentValue.DelayMaxTime; i++)
        {
            await Task.Delay(_openAiOptionsMonitor.CurrentValue.DelayMillisecond);
            transactionResultDto = await _contractProvider.QueryTransactionResult(transactionId, chainId);
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
            var tuple = await _aiArtProvider.GetAIImageListAsync(new SearchAIArtsInput()
            {
                Address = currentAddress,
                SkipCount = 0,
                MaxResultCount = input.ImageList.Count,
                Status = (int)AiImageUseStatus.UNUSE,
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
                imageIndex.status = (int)AiImageUseStatus.USE;
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
}