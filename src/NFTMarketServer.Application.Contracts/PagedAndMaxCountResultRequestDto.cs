using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Helper;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer;

public class PagedAndMaxCountResultRequestDto : PagedResultRequestDto
{
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        MaxMaxResultCount = ValidHelper.MaxResultNumber;
        return base.Validate(validationContext);
    }
}