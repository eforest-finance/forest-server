using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public class NFTDropQuotaDto :  EntityDto<string>
{
    public string DropId { get; set; }
    public long TotalAmount { get; set; }
    public long ClaimAmount { get; set; }
    public long AddressClaimLimit { get; set; }
    public long AddressClaimAmount { get; set; }
    public NFTDropState State { get; set; }
}