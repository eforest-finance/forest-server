using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace NFTMarketServer.NFT.Dto;

public class GetIssuedCountInput
{
    public string NFTInfoId { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (string.IsNullOrWhiteSpace(NFTInfoId))
        {
            yield return new ValidationResult($"Invalid NFT info id: {NFTInfoId}");
        }
    }
}