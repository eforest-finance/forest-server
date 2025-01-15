using System.Collections.Generic;

namespace NFTMarketServer.ThirdToken;

public class MyThirdTokenDto
{
    public string AelfChain { get; set; }
    public string AelfToken { get; set; }
    public string ThirdChain { get; set; }
    public string ThirdTokenName { get; set; }
    public string ThirdSymbol { get; set; }
    public string ThirdTokenImage { get; set; }
    public string ThirdContractAddress { get; set; }
    public long ThirdTotalSupply { get; set; }
    public string Address { get; set; }
}

public class MyThirdTokenResult
{
    public List<MyThirdTokenDto> Items { get; set; }
    public long TotalCount { get; set; }
}