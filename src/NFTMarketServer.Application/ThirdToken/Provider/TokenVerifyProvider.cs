using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Common.Http;
using NFTMarketServer.Grains.Grain.ThirdToken;
using NFTMarketServer.Options;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.ThirdToken.Provider;

public interface ITokenVerifyProvider
{
    Task AutoVerifyTokenAsync(ThirdTokenGrainDto dto);
}

public class TokenVerifyProvider : ITokenVerifyProvider, ISingletonDependency
{
    private readonly IHttpService _httpService;
    private readonly IOptionsMonitor<ThirdTokenInfosOptions> _thirdTokenInfosOptionsMonitor;

    public TokenVerifyProvider(IHttpService httpService,
        IOptionsMonitor<ThirdTokenInfosOptions> thirdTokenInfosOptionsMonitor)
    {
        _httpService = httpService;
        _thirdTokenInfosOptionsMonitor = thirdTokenInfosOptionsMonitor;
    }


    public async Task AutoVerifyTokenAsync(ThirdTokenGrainDto dto)
    {
        var msg =
            $"Please Verify Token! \n Token Address: {dto.TokenContractAddress}.\n Token Chain: {dto.Chain}.\n Token Name: {dto.TokenName}.\n Token Symbol: {dto.Symbol}.\n AssociatedTokenAccount : {dto.AssociatedTokenAccount}.\n";
        var content = new Dictionary<string, string>()
        {
            ["text"] = msg,
        };
        var param = new Dictionary<string, string>
        {
            ["msg_type"] = "text",
            ["content"] = JsonConvert.SerializeObject(content)
        };
        var requestBody = JsonConvert.SerializeObject(param);
        var header = new Dictionary<string, string>();
        await _httpService.SendPostRequest(_thirdTokenInfosOptionsMonitor.CurrentValue.AutoVerifyUrl, requestBody,
            header, 3);
    }
}