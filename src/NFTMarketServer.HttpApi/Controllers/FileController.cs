using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NFTMarketServer.File;
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
    public async Task<string> UpdateImage([Required] IFormFile file)
    {
        try
        {
            if (file == null || file.Length == 0)
            {
                _logger.LogError("UpdateImage: File is null or empty");
                return "";
            }

            string extension = Path.GetExtension(file.FileName).ToLower();
            string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
            if (!allowedExtensions.Contains(extension))
            {
                _logger.LogError("UpdateImage: File type is not allowed");
                return "";
            }

            await using var stream = file.OpenReadStream();
            byte[] utf8Bytes = stream.GetAllBytes();
            return await _symbolIconAppService.UpdateNFTIconAsync(utf8Bytes,
                DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + "-" + file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateImage: An unexpected error occurred - {Message}", ex.Message);
            return "";
        }
    }
}