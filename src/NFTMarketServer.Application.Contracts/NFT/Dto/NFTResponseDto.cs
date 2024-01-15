namespace NFTMarketServer.NFT.Dto;

public class GetIssuedCountResponse
{
    public long IssuedCount { get; set; } = 0;
    public long SupplyCount { get; set; } = 0;
    public long TotalSupplyCount { get; set; } = 0;
}