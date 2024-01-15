using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.Options;
using NFTMarketServer.Order.Dto;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Order;

public interface IPortkeyClientProvider
{
    Task<PortkeyCreateOrderResultDto> CreateOrderAsync(PortkeyCreateOrderParam param);
    Task<PortkeySearchOrderResultDto> SearchOrderAsync(PortkeySearchOrderParam param);
    Task<bool> NotifyReleaseResultAsync(PortkeyNotifyReleaseResultParam param);
}

public class PortkeyClientProvider : IPortkeyClientProvider, ISingletonDependency
{
    private readonly HttpClient _httpClient;
    private readonly IOptionsMonitor<PortkeyOption> _portkeyOptionsMonitor;
    private readonly ILogger<PortkeyClientProvider> _logger;

    public PortkeyClientProvider(IOptionsMonitor<PortkeyOption> portkeyOptionsMonitor, 
        ILogger<PortkeyClientProvider> logger)
    {
        _httpClient = new HttpClient();
        _portkeyOptionsMonitor = portkeyOptionsMonitor;
        _logger = logger;
    }

    public async Task<PortkeyCreateOrderResultDto> CreateOrderAsync(PortkeyCreateOrderParam param)
    {
        var portkeyOption = _portkeyOptionsMonitor.CurrentValue;
        param.Signature = portkeyOption.PrivateKey.GetSignature(param);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, portkeyOption.CreateOrderUrl);
        var jsonString = ConvertObjectToJsonString(param);
        HttpContent httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        httpRequest.Content = httpContent;
        var response = await _httpClient.SendAsync(httpRequest);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("url:{url},reqBody:{jsonString},result:{content}",
                portkeyOption.CreateOrderUrl, jsonString, content);
            var result = JsonConvert.DeserializeObject<BasicResult<PortkeyCreateOrderResultDto>>(content);
            AssertHelper.IsTrue(result.Code == BasicStatusCode.Success, "creat order fail:" + result.Code);

            var createOrderResultDto = result.Data;
            AssertHelper.IsTrue(portkeyOption.PublicKey.VerifySignature(createOrderResultDto.Signature, createOrderResultDto), "VerifySignature fail");
            return createOrderResultDto;
        }
        _logger.LogError("url:{url},reqBody:{jsonString},httpStatus:{code}",
            portkeyOption.CreateOrderUrl, jsonString, response.StatusCode);
        return null;
    }

    public async Task<PortkeySearchOrderResultDto> SearchOrderAsync(PortkeySearchOrderParam param)
    {
        var portkeyOption = _portkeyOptionsMonitor.CurrentValue;
        param.Signature = HttpUtility.UrlEncode(portkeyOption.PrivateKey.GetSignature(param));
        var jsonObject = param.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(param, null));
        var rawData = string.Join("&", jsonObject.Select(kv => $"{kv.Key}={kv.Value}"));
        var request = new HttpRequestMessage(HttpMethod.Get, portkeyOption.SearchOrderUrl + "?" + rawData);
        var response = await _httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("url:{url},result:{content}",
                request.RequestUri,  content);
            var result = JsonConvert.DeserializeObject<BasicResult<Dictionary<string, object>>>(content);
            AssertHelper.IsTrue(result.Code == BasicStatusCode.Success, "search order fail:" + result.Code);
            var searchOrderResultDto = result.Data.ToObject<PortkeySearchOrderResultDto>();
            AssertHelper.IsTrue(portkeyOption.PublicKey.VerifySignature(searchOrderResultDto.Signature, result.Data), "VerifySignature fail");
            return searchOrderResultDto;
        }
        _logger.LogError("url:{url},httpStatus:{code}",request.RequestUri, response.StatusCode);
        return null;
    }

    public async Task<bool> NotifyReleaseResultAsync(PortkeyNotifyReleaseResultParam param)
    {
        var portkeyOption = _portkeyOptionsMonitor.CurrentValue;
        param.Signature = portkeyOption.PrivateKey.GetSignature(param);
        var httpRequest = new HttpRequestMessage(HttpMethod.Post, portkeyOption.NotifyReleaseUrl);
        var jsonString = ConvertObjectToJsonString(param);
        HttpContent httpContent = new StringContent(jsonString, Encoding.UTF8, "application/json");
        httpRequest.Content = httpContent;
        var response = await _httpClient.SendAsync(httpRequest);
        if (response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger.LogInformation("url:{url},reqBody:{jsonString},result:{content}",
                portkeyOption.CreateOrderUrl, jsonString, content);
            var result = JsonConvert.DeserializeObject<BasicResult<Dictionary<string, string>>>(content);
            AssertHelper.IsTrue(result.Code == BasicStatusCode.Success, "NotifyRelease fail:" + result.Code);
            return true;
        }
        _logger.LogError("url:{url},reqBody:{jsonString},httpStatus:{code}",
            portkeyOption.CreateOrderUrl, jsonString, response.StatusCode);
        return false;
    }
    
    private string ConvertObjectToJsonString<T>(T paramObj)
    {
        var paramMap = paramObj.GetType().GetProperties().ToDictionary(p => p.Name, p => p.GetValue(paramObj, null));
        return JsonConvert.SerializeObject(paramMap);
    }
}