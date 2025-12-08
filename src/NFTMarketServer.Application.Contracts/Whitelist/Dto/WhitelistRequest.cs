using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Common;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Whitelist.Dto;

public class GetWhitelistByHashDto
{
    [Required] public string ChainId { get; set; }
    [Required] public string WhitelistHash { get; set; }
}

public class GetWhitelistExtraInfoListDto : PagedResultRequestDto, IValidatableObject
{
    [Required] public string ChainId { get; set; }
    [Required] public string ProjectId { get; set; }
    [Required] public string WhitelistHash { get; set; }
    [Required] public string CurrentAddress { get; set; }
    public string SearchAddress { get; set; }
    public string TagHash { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidateHelper.IsContainsNumbers(ChainId))
        {
            yield return new ValidationResult("The chainId is incorrectly, there should be no numbers.");
        }

        if (ValidateHelper.IsContainsSpecialCharacter(ProjectId) ||
            ValidateHelper.IsContainsSpecialCharacter(WhitelistHash) ||
            ValidateHelper.IsContainsSpecialCharacter(CurrentAddress))
        {
            yield return new ValidationResult("Invalid hash input, special characters are not allowed in parameters.");
        }
    }
}

public class GetWhitelistManagerListDto : PagedResultRequestDto, IValidatableObject
{
    [Required] public string ChainId { get; set; }
    [Required] public string ProjectId { get; set; }
    [Required] public string WhitelistHash { get; set; }
    [Required] public string Address { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidateHelper.IsContainsNumbers(ChainId))
        {
            yield return new ValidationResult("The chainId is incorrectly, there should be no numbers.");
        }

        if (ValidateHelper.IsContainsSpecialCharacter(ProjectId) ||
            ValidateHelper.IsContainsSpecialCharacter(WhitelistHash) ||
            ValidateHelper.IsContainsSpecialCharacter(Address))
        {
            yield return new ValidationResult("Invalid hash input, special characters are not allowed in parameters.");
        }
    }
}

public class GetTagInfoListDto : PagedResultRequestDto, IValidatableObject
{
    [Required] public string ChainId { get; set; }
    [Required] public string ProjectId { get; set; }
    [Required] public string WhitelistHash { get; set; }
    public long PriceMin { get; set; }
    public long PriceMax { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidateHelper.IsContainsNumbers(ChainId))
        {
            yield return new ValidationResult("The chainId is incorrectly, there should be no numbers.");
        }

        if (ValidateHelper.IsContainsSpecialCharacter(ProjectId) ||
            ValidateHelper.IsContainsSpecialCharacter(WhitelistHash))
        {
            yield return new ValidationResult("Invalid hash input, special characters are not allowed in parameters.");
        }

        if (PriceMin < 0 || PriceMax < 0 || PriceMin > PriceMax)
        {
            yield return new ValidationResult($"Invalid Price input: PriceMax:{PriceMax}, PriceMin:{PriceMin}.");
        }
    }
}

public class GetPriceTokenListDto : IValidatableObject
{
    [Required] public string ChainId { get; set; }
    [Required] public string WhitelistHash { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (ValidateHelper.IsContainsNumbers(ChainId))
        {
            yield return new ValidationResult("The chainId is incorrectly, there should be no numbers.");
        }

        if (ValidateHelper.IsContainsSpecialCharacter(WhitelistHash))
        {
            yield return new ValidationResult("Invalid hash input, special characters are not allowed in parameters.");
        }
    }
}