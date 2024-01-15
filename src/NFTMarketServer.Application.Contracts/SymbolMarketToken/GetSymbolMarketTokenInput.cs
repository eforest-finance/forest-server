using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.SymbolMarketToken;

public class GetSymbolMarketTokenInput : PagedAndMaxCountResultRequestDto
{
    [Required] public List<string> AddressList { get; set; }

    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        base.Validate(validationContext);
        for (int i = 0; i < AddressList?.Count; i++)
        {
            if (!AddressList[i].MatchesAddress())
            {
                yield return new ValidationResult(
                    BasicStatusMessage.IllegalInputData,
                    new[] { "address" }
                );
            }
        }
    }
}