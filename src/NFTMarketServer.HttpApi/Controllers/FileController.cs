using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
}