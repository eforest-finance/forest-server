using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NFTMarketServer.Common.Http;
using NFTMarketServer.Options;
using NFTMarketServer.ThirdToken.Index;

namespace NFTMarketServer.ThirdToken.Strategy;

public class SolanaTokenStrategy : IThirdTokenStrategy
{
    private readonly IHttpService _httpService;

    public SolanaTokenStrategy(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<bool> CheckThirdTokenExistAsync(string tokenName, string tokenSymbol, string deployedAddress,
        string associatedTokenAccount, ThirdTokenInfo info, string abi)
    {
        var param = new SolanaRequest
        {
            JsonRpc = "2.0",
            Id = 1,
            Method = "getTokenSupply",
            Params = [associatedTokenAccount]
        };
        var requestBody = JsonConvert.SerializeObject(param);
        var header = new Dictionary<string, string>();
        var res = await _httpService.SendPostRequest(info.Url, requestBody, header, 3);
        var result = JsonConvert.DeserializeObject<SolanaResponse>(res);

        return result.Error == null && result.Result.Value.Amount != "0";
    }

    public new ThirdTokenType GetThirdTokenType()
    {
        return ThirdTokenType.Solana;
    }
}