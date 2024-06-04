using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using NFTMarketServer.AwsS3;
using NFTMarketServer.Icon;

namespace NFTMarketServer.File;

public class SymbolIconAppService : NFTMarketServerAppService, ISymbolIconAppService
{
    private readonly AwsS3Client _awsS3Client;
    private readonly ISymbolIconProvider _symbolIconProvider;


    private const string SvgBackGroundData = @"<?xml version=""1.0"" encoding=""utf-8""?>
        <!-- Generator: Adobe Illustrator 27.8.1, SVG Export Plug-In . SVG Version: 6.00 Build 0)  -->
        <svg version=""1.1"" id=""picture_1"" xmlns=""http://www.w3.org/2000/svg"" xmlns:xlink=""http://www.w3.org/1999/xlink"" x=""0px"" y=""0px""
         viewBox=""0 0 1000 1000"" style=""enable-background:new 0 0 1000 1000;"" xml:space=""preserve"">
        <style type=""text/css"">
            .st0{fill:#8B60F7;}
            .st1{opacity:0.2;}
            .st2{fill:#FFFFFF;}
        </style>
        <rect class=""st0"" width=""1000"" height=""1000""/>
        <g class=""st1"">
            <circle class=""st2"" cx=""179.5"" cy=""504.5"" r=""104.5""/>
        <g>
                <path class=""st2"" d=""M0,406c33-60,95.5-101.5,167.5-106c-12.5-30-21.5-62-26-95.5c-52.5,7-101,27-141.5,57V406z""/>
                <path class=""st2"" d=""M393,290.5L393,290.5c-37-37.5-60-89-60-145.5S356,37.5,393,0.5L392.5,0H273c-23.5,43-37,92.5-37,145.5
                        c0,83.5,34,159.5,89,214l-0.5,0.5c37,37,60,88.5,60,145c0,113-92,204.5-204.5,204.5c-77,0-144.5-43-179.5-106v145
                        c50,37,112,59,179.5,59c167,0,302.5-135.5,302.5-302C481.5,421,448,345.5,393,290.5z""/>
        </g>
        </g>";

    private const string SvgBackGroundDataEnd = "</svg>";

    private const string SvgBackGroundDataText =
        @"<text x=""500"" y=""500"" text-anchor=""middle"" dominant-baseline=""middle"" font-family=""Helvetica"" font-size=""{0}"" style=""fill:white;"">{1}</text>";

    public SymbolIconAppService(AwsS3Client awsS3Client,
        ISymbolIconProvider symbolIconProvider
    )
    {
        _awsS3Client = awsS3Client;
        _symbolIconProvider = symbolIconProvider;
    }

    public async Task<string> UpLoadIconAsync(Stream stream, string seedSymbol, string symbol)
    {
        var fileUrl = await _symbolIconProvider.GetIconBySymbolAsync(seedSymbol);
        if (!string.IsNullOrEmpty(fileUrl))
        {
            return fileUrl;
        }

        fileUrl = await _awsS3Client.UpLoadFileAsync(stream, seedSymbol);
        await _symbolIconProvider.AddSymbolIconAsync(seedSymbol, fileUrl);
        return fileUrl;
    }


    public async Task<string> GetIconBySymbolAsync(string seedSymbol, string symbol)
    {
        if (seedSymbol.IsNullOrEmpty())
        {
            throw new Exception($"seedSymbol is null,symbol:{symbol}");
        }

        var fileUrl = await _symbolIconProvider.GetIconBySymbolAsync(seedSymbol);
        if (!string.IsNullOrEmpty(fileUrl))
        {
            return fileUrl;
        }

        var waterMarkImageStream = AddWaterMarkByStream(symbol);
        var upLoadIcon = await UpLoadIconAsync(waterMarkImageStream, seedSymbol, symbol);
        return upLoadIcon;
    }

    public async Task<string> UpdateNFTIconAsync(byte[] utf8Bytes, string symbol)
    {
        var stream = new MemoryStream(utf8Bytes);
        return await _awsS3Client.UpLoadFileForNFTAsync(stream, symbol);
    }
    
    public async Task<KeyValuePair<string,string>> UpdateNFTIconWithHashAsync(byte[] utf8Bytes, string symbol)
    {
        var stream = new MemoryStream(utf8Bytes);
        return await _awsS3Client.UpLoadFileForNFTWithHashAsync(stream, symbol);
    }

    private Stream AddWaterMarkByStream(string symbol)
    {
        var newSymbol = "SEED-" + symbol;
        var size = GetFontSize(newSymbol);
        var text = string.Format(SvgBackGroundDataText, size, newSymbol);
        var data = SvgBackGroundData + text + SvgBackGroundDataEnd;
        var byteArray = Encoding.UTF8.GetBytes(data);
        var stream = new MemoryStream(byteArray);
        return stream;
    }


    private int GetFontSize(string symbol)
    {
        if (symbol.Length <= 5) return 156;
        if (symbol.Length <= 10) return 120;
        if (symbol.Length <= 15) return 80;
        if (symbol.Length <= 20) return 60;
        if (symbol.Length <= 25) return 48;
        if (symbol.Length <= 30) return 40;
        return 38;
    }
}