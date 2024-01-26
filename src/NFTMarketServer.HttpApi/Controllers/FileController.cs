using System;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NFTMarketServer.File;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;
using Volo.Abp.Content;

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

    // [HttpPost]
    // [Route("update-image")]
    // public async Task<string> UpdateImage([Required] IFormFile file)
    // {
    //     try
    //     {
    //         // Check if the file is null or empty
    //         if (file == null || file.Length == 0)
    //         {
    //             _logger.LogError("UpdateImage: File is null or empty");
    //             return "File is null or empty";
    //         }
    //
    //         // Validate file type
    //         string extension = Path.GetExtension(file.FileName).ToLower();
    //         string[] allowedExtensions = { ".jpg", ".jpeg", ".png" };
    //         if (!allowedExtensions.Contains(extension))
    //         {
    //             _logger.LogError("UpdateImage: File type is not allowed");
    //             return "File type is not allowed";
    //         }
    //
    //         // Use a using statement to ensure proper disposal of resources
    //         using (var stream = file.OpenReadStream())
    //         {
    //             // Now you can use stream directly for further processing
    //             return await _symbolIconAppService.UpdateNFTIconAsync(stream, file.FileName);
    //         }
    //     }
    //     catch (Exception ex)
    //     {
    //         _logger.LogError($"UpdateImage: An unexpected error occurred - {ex.Message}");
    //         return "An unexpected error occurred";
    //     }
    // }

    [HttpPost]
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
            return await _symbolIconAppService.UpdateNFTIconAsync(utf8Bytes, file.FileName);
        }
        catch (Exception ex)
        {
            _logger.LogError("UpdateImage: An unexpected error occurred - {Message}", ex.Message);
            return "";
        }
    }
}