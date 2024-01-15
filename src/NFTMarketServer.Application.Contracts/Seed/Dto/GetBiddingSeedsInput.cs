using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Seed.Dto;

public class GetBiddingSeedsInput : PagedResultRequestDto
{
    public bool LiveAuction { get; set; }
    public List<string> ChainIds { get; set; }
    [Range(0, 30, ErrorMessage = "The SymbolLengthMin must be between 0 and 30.")]
    public int? SymbolLengthMin { get; set; }
    [Range(0, 30, ErrorMessage = "The SymbolLengthMax must be between 0 and 30.")]
    public int? SymbolLengthMax { get; set; }
    [Range(0, long.MaxValue, ErrorMessage = "The PriceMin must be greater than 0.")]
    public long? PriceMin { get; set; }
    [Range(0, long.MaxValue, ErrorMessage = "The PriceMax must be greater than 0.")]
    public long? PriceMax { get; set; }
    public List<TokenType> TokenTypes { get; set; }
    public List<SeedType> SeedTypes { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (SymbolLengthMin > SymbolLengthMax)
        {
            yield return new ValidationResult($"SymbolLengthMin must be less than SymbolLengthMax.");
        }

        if (PriceMin > PriceMax)
        {
            yield return new ValidationResult($"PriceMin must be less than PriceMax.");
        }
    }
}