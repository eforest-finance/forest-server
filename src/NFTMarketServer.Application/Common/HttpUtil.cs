using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Newtonsoft.Json;
using NFTMarketServer.HandleException;

namespace NFTMarketServer.Common;

public class HttpUtil
{
    private static readonly HttpClientHandler _handler = new HttpClientHandler
    {
        ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
    };

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(60);
    private const int MaxRetryCount = 3;
    private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(1);

    public static async Task<byte[]> DownloadImageAsUtf8BytesWithRetryAsync(string url,int retryNumber = MaxRetryCount)
    {
        return await SendRequestWithRetry(async () => await DownloadImageAsUtf8BytesAsync(url), retryNumber);
    }

    public static async Task<byte[]> DownloadImageAsUtf8BytesAsync(string url)
    {
        using (var httpClient = new HttpClient())
        {
            using (var response = await httpClient.GetAsync(url))
            {
                response.EnsureSuccessStatusCode();
                using (var stream = await response.Content.ReadAsStreamAsync())
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
        }
    }


    public static async Task<string> SendPostRequest(string apiUrl, string requestBody,
        Dictionary<string, string> header,int maxRetryCount = MaxRetryCount)
    {
        return await SendRequestWithRetry(async () => await PostRequest(apiUrl, requestBody, header), maxRetryCount);
    }

    public static async Task<string> SendPostFormRequest(string apiUrl, Dictionary<string, string> parameters,
        Dictionary<string, string> header)
    {
        return await SendRequestWithRetry(async () => await PostRequest(apiUrl, parameters, header));
    }

    public static async Task<string> SendGetRequest(string apiUrl, Dictionary<string, string> header)
    {
        return await SendRequestWithRetry(async () => await GetRequest(apiUrl, header));
    }

    private static async Task<byte[]> SendRequestWithRetry(Func<Task<byte[]>> requestAction,int maxRetryCount = MaxRetryCount)
    {
        var currentRetry = 0;
        return await requestAction();
    }
    

    private static async Task<string> SendRequestWithRetry(Func<Task<string>> requestAction,int maxRetryCount = MaxRetryCount)
    {
        var currentRetry = 0;

        return await requestAction();
    }
    [ExceptionHandler(typeof(Exception), 
        Message = "HttpUtils.PostRequest PostRequest is fail", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"apiUrl", "requestBody", "header", "mediaType"})]
    public  static  async Task<string> PostRequest(string apiUrl, string requestBody,
        Dictionary<string, string> header, string mediaType = "application/json")
    {
        var client = CreateHttpClient(header);
        {
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            var content = new StringContent(requestBody, Encoding.UTF8, mediaType);
            var response = await client.PostAsync(apiUrl, content);
            if (response != null && response.IsSuccessStatusCode)
            {
                return await response.Content.ReadAsStringAsync();
            }
                
            return await response.Content.ReadAsStringAsync();
        }
    }
    [ExceptionHandler(typeof(Exception),
        Message = "HttpUtils.PostRequest HTTP POST Exception is fail", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"apiUrl", "parameters", "header"})]
    public static async Task<string> PostRequest(string apiUrl, Dictionary<string, string> parameters,
        Dictionary<string, string> header)
    {
        var client = CreateHttpClient(header);
        client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };

        var content = new FormUrlEncodedContent(parameters);

        var response = await client.PostAsync(apiUrl, content);

        return await response?.Content?.ReadAsStringAsync();
    }
    [ExceptionHandler(typeof(Exception),
        Message = "HttpUtils.GetRequest fail", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"apiUrl"})]
    public static async Task<string> GetRequest(string apiUrl, Dictionary<string, string> header)
    {
        var client = CreateHttpClient(header);
        {
            client.DefaultRequestHeaders.CacheControl = new CacheControlHeaderValue { NoCache = true };
            HttpResponseMessage response = await client.GetAsync(apiUrl);

            if (response != null && response.IsSuccessStatusCode)
            {
                string responseBody = await response.Content.ReadAsStringAsync();
                return responseBody;
            }

            return JsonConvert.SerializeObject(response);
        }
    }

    private static HttpClient CreateHttpClient(Dictionary<string, string> header)
    {
        var client = new HttpClient(_handler)
        {
            Timeout = Timeout
        };

        client.DefaultRequestHeaders.Clear();
        foreach (var keyValuePair in header)
        {
            if (client.DefaultRequestHeaders.Contains(keyValuePair.Key)) continue;
            client.DefaultRequestHeaders.Add(keyValuePair.Key, keyValuePair.Value);
        }

        return client;
    }
}