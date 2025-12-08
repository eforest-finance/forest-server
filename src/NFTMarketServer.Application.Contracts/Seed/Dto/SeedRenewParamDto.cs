namespace NFTMarketServer.Seed.Dto;

public class SeedRenewParamDto
{
    public string Buyer { get; set; }
    public string SeedSymbol { get; set; }
    public string PriceSymbol { get; set; }
    public long PriceAmount { get; set; }
    public long OpTime { get; set; }
    public string RequestHash { get; set; }

}