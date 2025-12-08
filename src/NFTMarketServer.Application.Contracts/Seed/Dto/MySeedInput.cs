using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.Seed.Dto;

public class MySeedInput : PagedAndMaxCountResultRequestDto
{
    public string ChainId { get; set; }
    public List<string> Address { get; set; }
    public TokenType? TokenType { get; set; }
    public SeedStatus? Status { get; set; }
    
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        base.Validate(validationContext);
        for (int i = 0; i < Address?.Count; i++)
        {
            if (!Address[i].MatchesAddress())
            {
                yield return new ValidationResult(
                    BasicStatusMessage.IllegalInputData,
                    new[] { "address" }
                );
            }
        }
    }
}