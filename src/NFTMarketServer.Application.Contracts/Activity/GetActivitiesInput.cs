using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.Activity;

public class GetActivitiesInput : PagedAndMaxCountResultRequestDto
{
    [Required] public string Address { get; set; }
    [Required] public List<SymbolMarketActivityType> Types { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        base.Validate(validationContext);

        if (!Address.MatchesAddress())
        {
            yield return new ValidationResult(
                BasicStatusMessage.IllegalInputData,
                new[] { "address" }
            );
        }
    }
}