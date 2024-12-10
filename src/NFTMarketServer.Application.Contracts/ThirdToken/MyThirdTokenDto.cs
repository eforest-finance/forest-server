using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.ThirdToken;

public class MyThirdTokenDto : EntityDto<string>
{
    public string AelfChain { get; set; }
    public string AelfToken { get; set; }
    public string ThirdChain { get; set; }
    public string ThirdTokenName { get; set; }
    public string ThirdSymbol { get; set; }
    public string ThirdTokenImage { get; set; }
    public string ThirdContractAddress { get; set; }
    public long ThirdTotalSupply { get; set; }
}