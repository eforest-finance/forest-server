using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.ThirdToken;

public class ThirdTokenPrepareBindingDto : EntityDto<string>
{
    public string BindingId { get; set; }
    public string ThirdTokenId { get; set; }
}