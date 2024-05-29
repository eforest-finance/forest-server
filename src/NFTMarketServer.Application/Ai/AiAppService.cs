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
using NFTMarketServer.Common;
using NFTMarketServer.Common.AElfSdk;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Provider;
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


    public AiAppService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<OpenAiOptions> openAiOptionsMonitor,
        ILogger<AiAppService> logger,
        IContractProvider contractProvider,
        ISymbolIconAppService symbolIconAppService,
        IUserAppService userAppService,
        INESTRepository<AiCreateIndex, string> aiCreateIndexRepository,
        INESTRepository<AIImageIndex, string> aIImageIndexRepository,
        IAIArtProvider aiArtProvider
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

    }

    public async Task<PagedResultDto<string>> CreateAiArtAsync(CreateAiArtInput input)
    {
        var chainId = input.ChainId;
        string transactionId;
        
        CreateArtInput createArtInput;
        Transaction transaction;
        AiCreateIndex aiCreateIndex;
        try
        {
            transaction = TransferHelper.TransferToTransaction(input.RawTransaction);
            createArtInput = TransferToCreateArtInput(transaction, chainId);
            transactionId = await SendTransactionAsync(chainId, transaction);
            aiCreateIndex = BuildAiCreateIndex(transactionId, transaction, createArtInput);
            await _aiCreateIndexRepository.AddAsync(aiCreateIndex);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SendTransactionAsync error");
            throw new SystemException(e.Message);
        }

        var s3UrlList = await GenerateImageAsync(createArtInput, transaction, transactionId, aiCreateIndex);
        
        return new PagedResultDto<string>()
        {
            TotalCount = s3UrlList.Count,
            Items = s3UrlList
        };
    }
    
    private static AiCreateIndex BuildAiCreateIndex(string transactionId,Transaction transaction,CreateArtInput createArtInput)
    {
        return new AiCreateIndex
        {
            Id = IdGenerateHelper.GetAiCreateId(transactionId, transaction.From.ToBase58()),
            TransactionId = transactionId,
            Address = transaction.From.ToBase58(),
            Ctime = DateTime.UtcNow,
            Utime = DateTime.UtcNow,
            Model = createArtInput.Model.ToEnum<AiModelType>(),
            NegativePrompt = createArtInput.NegativePrompt,
            Number = createArtInput.Number,
            PaintingStyle = createArtInput.PaintingStyle.ToEnum<AiPaintingStyleType>(),
            Promt = createArtInput.Promt,
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
    private async Task<List<string>> GenerateImageAsync(CreateArtInput createArtInput, Transaction transaction,
        string transactionId, AiCreateIndex aiCreateIndex)
    {
        var openAiUrl = _openAiOptionsMonitor.CurrentValue.ImagesUrlV1;
        var openAiRequestBody = JsonConvert.SerializeObject(new OpenAiImageGenerationDto
        {
            Model = createArtInput.Model,
            N = createArtInput.Number,
            Prompt = createArtInput.Promt + ".Image style:" + createArtInput.PaintingStyle + ". without:" +
                     createArtInput.NegativePrompt,
            Size = createArtInput.Size
        });
        var openAiHeader = new Dictionary<string, string>
        {
            [CommonConstant.Authorization] = CommonConstant.BearerToken + _openAiOptionsMonitor.CurrentValue.ApiKeyList[0]
        };

        var result = new OpenAiImageGenerationResponse();
        var retryCount = 1;
        var openAiMsg = "";
        for (;retryCount <= 3; retryCount++)
        {
            var openAiResult =
                await HttpUtil.SendPostRequest(openAiUrl, openAiRequestBody, openAiHeader, CommonConstant.IntOne);
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
            throw new SystemException("Ai Image Generation Error");
        }
        else
        {
            aiCreateIndex.Status = AiCreateStatus.IMAGECREATED;
            await _aiCreateIndexRepository.UpdateAsync(aiCreateIndex);
        }
        
        
        var s3UrlList = new List<string>();
        var addList = new List<AIImageIndex>();
        for (var j = 0; j < result.Data.Count; j++)
        {
            var openAiImageGeneration = result.Data[j];
            var imageBytes = await HttpUtil.DownloadImageAsUtf8BytesAsync(openAiImageGeneration.Url);

            try
            {
                var s3Url = await _symbolIconAppService.UpdateNFTIconAsync(imageBytes,
                    transactionId + CommonConstant.Underscore + transaction.From.ToBase58() + CommonConstant.Underscore +
                    DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + CommonConstant.ImagePNG);
                s3UrlList.Add(s3Url);
                addList.Add(new AIImageIndex
                {
                    Id = IdGenerateHelper.GetAIImageId(transactionId,transaction.To.ToBase58(),j),
                    Address = transaction.From.ToBase58(),
                    Ctime = DateTime.UtcNow,
                    Utime = DateTime.UtcNow,
                    S3Url = s3Url,
                    TransactionId = transactionId
                });
                
            }
            catch (Exception e)
            {
                _logger.LogError("s3 upload error openAiImageGeneration={A}", openAiImageGeneration.Url);
            }
        }
        await _aIImageIndexRepository.BulkAddOrUpdateAsync(addList);
        return s3UrlList;
    }

    private async Task<string> SendTransactionAsync(string chainId, Transaction transaction)
    {
        var transactionOutput = await _contractProvider.SendTransactionAsync(chainId, transaction);

        var transactionId = transactionOutput.TransactionId;

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

        if (!transactionResultDto.Status.Equals(TransactionState.Mined))
        {
            _logger.LogError("QueryTransactionResult is fail, transactionId={A} result={B}", transactionId,
                JsonConvert.SerializeObject(transactionResultDto));
            throw new SystemException("QueryTransactionResult is fail transactionId=" + transactionId);
        }

        return transactionId;
    }
    public async Task<PagedResultDto<List<string>>> GetAiArtsAsync(GetAIArtsInput input)
    {
        var currentUserAddress = "";
        _logger.LogInformation("GetCurrentUserAddress address:{}",currentUserAddress);

        try
        {
            currentUserAddress = await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress.IsNullOrEmpty())
            {
                _logger.LogError("GetCurrentUserAddress error");
                throw new UserFriendlyException("GetCurrentUserAddress error,Please log in again.");
            }
            var tuple = await _aiArtProvider.GetAIImageListAsync(new SearchAIArtsInput()
            {
                Address = currentUserAddress,
                SkipCount = input.SkipCount,
                MaxResultCount = input.MaxResultCount
            });

            if (tuple == null || tuple.Item1 == 0)
            {
                return new PagedResultDto<List<string>>();
            }

            var artList = tuple.Item2;
            return new PagedResultDto<List<string>>()
            {
                TotalCount = tuple.Item1,
                Items = new[] {artList.Select(r=>r.S3Url).ToList()}
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e,"GetAiArtsAsync Exception: user:{address}",currentUserAddress);
            throw new UserFriendlyException("GetAiArtsAsync Exception: user:{address} e:{error}",currentUserAddress, e.Message);
        }
    }
}