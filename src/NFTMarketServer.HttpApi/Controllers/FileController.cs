using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NFTMarketServer.File;
using NFTMarketServer.HandleException;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace NFTMarketServer.Controllers;

public class FileInput
{
    public string seedSymbol { get; set; }
    public string symbol { get; set; }
}

[RemoteService]
[Area("app")]
[ControllerName("File")]
[Route("api/app/icon")]
public class FileController : AbpController
{
    private readonly ISymbolIconAppService _symbolIconAppService;

    private readonly ILogger<FileController> _logger;

    public FileController(ISymbolIconAppService symbolIconAppService, ILogger<FileController> logger)
    {
        _symbolIconAppService = symbolIconAppService;
        _logger = logger;
    }


    [HttpPost]
    [Route("url")]
    public async Task<string> GetUrl(FileInput fileInput)
    {
        var result = await _symbolIconAppService.GetIconBySymbolAsync(fileInput.seedSymbol, fileInput.symbol);
        return result;
    }

    [HttpPost]
    [Authorize]
    [Route("update-image")]
    [ExceptionHandler(typeof(Exception),
        Message = "FileController.UpdateImage is fail", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        ReturnDefault = ReturnDefault.Default,
        LogTargets = new []{"file"}
    )]
    
    public virtual async Task<string> UpdateImage([Required] IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            _logger.LogError("UpdateImage: File is null or empty");
            return "";
        }

        var extension = Path.GetExtension(file.FileName).ToLower();
        string[] allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif" };
        if (!allowedExtensions.Contains(extension))
        {
            _logger.LogError("UpdateImage: File type is not allowed");
            return "";
        }

        await using var stream = file.OpenReadStream();
        var utf8Bytes = stream.GetAllBytes();
        return await _symbolIconAppService.UpdateNFTIconAsync(utf8Bytes,
            "drop_" + DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "_" + file.FileName);
    }
    
    [HttpGet]
    [Route("random-image")]
    public async Task<string> RandomImage()
    {
        return await _symbolIconAppService.GetRandomImageAsync();
    }
}