using System.Collections.Generic;
using System.Threading.Tasks;

namespace NFTMarketServer.Common.Http;

public interface IHttpService
{
    Task<byte[]> DownloadImageAsUtf8BytesAsync(string url, int retryNMumber);

    Task<string> SendPostRequest(string apiUrl, string requestBody,
        Dictionary<string, string> header, int maxRetryCount);
}

public class HttpService : NFTMarketServerAppService, IHttpService
{
    public async Task<byte[]> DownloadImageAsUtf8BytesAsync(string url,int retryNMumber)
    {
        return await HttpUtil.DownloadImageAsUtf8BytesWithRetryAsync(url, retryNMumber);
    }

    public async Task<string> SendPostRequest(string apiUrl, string requestBody,
        Dictionary<string, string> header,int maxRetryCount)
    {
        return await HttpUtil.SendPostRequest(apiUrl, requestBody, header, maxRetryCount);
    }
}