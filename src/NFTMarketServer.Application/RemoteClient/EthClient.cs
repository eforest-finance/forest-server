using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.RemoteClient;

public class EthClient : ISingletonDependency
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<EthClient> _logger;

    public EthClient(ILogger<EthClient> logger)
    {
        _httpClient = new HttpClient();
        _logger = logger;
    }

    private const string URL =
        "https://api.etherscan.io/api?module=contract&action=getcontractcreation&contractaddresses={0}&apikey=YourApiKeyToken";

    public async Task<string> FetchContractOwnerAsync(string contractAddress)
    {
        var reqMessage = new HttpRequestMessage(HttpMethod.Get, string.Format(URL, contractAddress));
        var resMessage = await _httpClient.SendAsync(reqMessage);
        var responseBody = await resMessage.Content.ReadAsStringAsync();
        _logger.LogInformation("req:{0}, res:{1}", URL, responseBody);
        var jsonObject = JsonConvert.DeserializeObject<EthApiGetContractCreationDto>(responseBody);
        return jsonObject.Status == "1" ? jsonObject.result[0].ContractCreator : string.Empty;
    }
}