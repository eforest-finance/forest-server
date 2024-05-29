using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Common.AElfSdk;
using NFTMarketServer.File;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Users;
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


    public AiAppService(IOptionsMonitor<ChainOptions> chainOptionsMonitor,
        IOptionsMonitor<OpenAiOptions> openAiOptionsMonitor,
        ILogger<AiAppService> logger,
        IContractProvider contractProvider,
        ISymbolIconAppService symbolIconAppService,
        IUserAppService userAppService
    )
    {
        _chainOptionsMonitor = chainOptionsMonitor;
        _openAiOptionsMonitor = openAiOptionsMonitor;
        _logger = logger;
        _contractProvider = contractProvider;
        _symbolIconAppService = symbolIconAppService;
        _userAppService = userAppService;

    }

    public async Task<PagedResultDto<string>> CreateAiArtAsync(CreateAiArtInput input)
    {
        var chainId = input.ChainId;
        string transactionId;
        
        CreateArtInput createArtInput;
        Transaction transaction;
         try
         {
             transaction =
                 Transaction.Parser.ParseFrom(ByteArrayHelper.HexStringToByteArray(input.RawTransaction));
             if (transaction.MethodName == "ManagerForwardCall")
             {
                 var managerForwardCallInput = ManagerForwardCallInput.Parser.ParseFrom(transaction.Params);
                 if (managerForwardCallInput.MethodName == "CreateArt")
                 {
                     createArtInput = CreateArtInput.Parser.ParseFrom(managerForwardCallInput.Args);
                 }
                 else
                 {
                     throw new UserFriendlyException("Invalid transaction");
                 }
             }
             else if (transaction.MethodName == "CreateArt")
             {
                 createArtInput = CreateArtInput.Parser.ParseFrom(transaction.Params);
             }
             else
             {
                 throw new UserFriendlyException("Invalid transaction");
             }
             transactionId = await SendTransactionAsync(chainId, transaction);

         }
         catch (Exception e)
         {
             _logger.LogError(e, "SendTransactionAsync error");
             throw new SystemException(e.Message);
         }

         var s3UrlList = await GenerateImageAsync(createArtInput, transaction, transactionId);
        
        return new PagedResultDto<string>()
        {
            TotalCount = s3UrlList.Count,
            Items = s3UrlList
        };
    }

    public async Task<PagedResultDto<List<string>>> GetAiArtsAsync()
    {
        var currentUserAddress = "";
        try
        {
            currentUserAddress = await _userAppService.GetCurrentUserAddressAsync();
            if (currentUserAddress.IsNullOrEmpty())
            {
                _logger.LogError("GetCurrentUserAddress error");
                throw new UserFriendlyException("GetCurrentUserAddress error,Please log in again.");
            }
            //query AIImageIndex
            var artUrlList = new List<string>();
            return new PagedResultDto<List<string>>()
            {
                TotalCount = artUrlList.Count,
                Items = new[] { artUrlList }
            };
        }
        catch (Exception e)
        {
            _logger.LogError(e,"GetAiArtsAsync Exception: user:{}",currentUserAddress);
            throw new UserFriendlyException("GetAiArtsAsync Exception: user:{} e:{}",currentUserAddress, e.Message);
        }
    }

    private async Task<List<string>> GenerateImageAsync(CreateArtInput createArtInput, Transaction transaction,
        string transactionId)
    {
        var openAiUrl = _openAiOptionsMonitor.CurrentValue.ImagesUrlV1;
        var openAiRequestBody = JsonConvert.SerializeObject(new OpenAiImageGenerationDto
        {
            Model = createArtInput.Model,
            N = createArtInput.Number,
            Prompt = createArtInput.Promt,
            Size = createArtInput.Size
        });
        var openAiHeader = new Dictionary<string, string>
        {
            [CommonConstant.Authorization] = CommonConstant.BearerToken + _openAiOptionsMonitor.CurrentValue.ApiKeyList[0]
        };

        var result = new OpenAiImageGenerationResponse();
        for (var i = 0; i < 3; i++)
        {
            var openAiResult =
                await HttpUtil.SendPostRequest(openAiUrl, openAiRequestBody, openAiHeader, CommonConstant.IntOne);
            try
            {
                result = JsonConvert.DeserializeObject<OpenAiImageGenerationResponse>(openAiResult);
            }
            catch (Exception e)
            {
                _logger.LogError(e,
                    "OpenAiImageGeneration Error Url={A} openAiRequestBody={B} result={C} createArtInput={D}",
                    openAiUrl,
                    openAiRequestBody, openAiResult, JsonConvert.SerializeObject(createArtInput));
            }

            if (result != null && result.Data?.Count > 0) break;
        }

        if (result == null || result.Data.IsNullOrEmpty())
        {
            throw new SystemException("Ai Image Generation Error");
        }

        var s3UrlList = new List<string>();
        foreach (var openAiImageGeneration in result.Data)
        {
            var imageBytes = await HttpUtil.DownloadImageAsUtf8BytesAsync(openAiImageGeneration.Url);

            var s3Url = await _symbolIconAppService.UpdateNFTIconAsync(imageBytes,
                transactionId + CommonConstant.Underscore + transaction.From.ToBase58() + CommonConstant.Underscore +
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + CommonConstant.ImagePNG);
            s3UrlList.Add(s3Url);
        }

        return s3UrlList;
    }

    private async Task<string> SendTransactionAsync(string chainId, Transaction transaction)
    {
        var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainId];
        var forestContractAddress = chainInfo?.ForestContractAddress;
        CreateArtInput createArtInput;
        if (!transaction.To.ToBase58().Equals(forestContractAddress) ||
            !transaction.MethodName.Equals("CreateArt"))
        {
            throw new UserFriendlyException("Invalid transaction");
        }

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
            throw new SystemException("QueryTransactionResult is fail");
        }

        return transactionId;
    }
}