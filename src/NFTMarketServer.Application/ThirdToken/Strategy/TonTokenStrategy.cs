using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using NFTMarketServer.Common.Http;
using NFTMarketServer.Options;
using NFTMarketServer.ThirdToken.Index;

namespace NFTMarketServer.ThirdToken.Strategy;

public class TonTokenStrategy : IThirdTokenStrategy
{
    private readonly IHttpService _httpService;

    public TonTokenStrategy(IHttpService httpService)
    {
        _httpService = httpService;
    }

    public async Task<bool> CheckThirdTokenExistAsync(string tokenName, string tokenSymbol, string deployedAddress,
        string associatedTokenAccount, ThirdTokenInfo info, string abi)
    {
        var header = new Dictionary<string, string>();
        var res = await _httpService.SendGetRequest(info.Url + deployedAddress, header);
        var result = JsonConvert.DeserializeObject<TonResponse>(res);

        return string.IsNullOrEmpty(result.Error) && result.Metadata.Symbol == tokenSymbol;
    }

    public new ThirdTokenType GetThirdTokenType()
    {
        return ThirdTokenType.Ton;
    }
}