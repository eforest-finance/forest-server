using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;

namespace NFTMarketServer.Users.Dto;

public class UserUpdateDto : IValidatableObject
{
    [Required]
    [MinLength(1), MaxLength(20)]
    public string Name { get; set; }

    [MaxLength(100)] public string Email { get; set; }
    [MaxLength(100)] public string Twitter { get; set; }
    [MaxLength(100)] public string Instagram { get; set; }
    public string ProfileImage { get; set; }
    public string ProfileImageOriginal { get; set; }
    public string BannerImage { get; set; }
    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (!RegexHelper.IsValid(Name, RegexType.UserName))
        {
            yield return new ValidationResult(
                BasicStatusMessage.IllegalInputData,
                new[] { "Name" }
            );
        }
        if (! RegexHelper.IsValid(Email,RegexType.Email))
        {
            yield return new ValidationResult(
                BasicStatusMessage.IllegalInputData,
                new[] { "email" }
            );
        }
        if (! RegexHelper.IsValid(Twitter,RegexType.Twitter))
        {
            yield return new ValidationResult(
                BasicStatusMessage.IllegalInputData,
                new[] { "twitter" }
            );
        }
        
        if (! RegexHelper.IsValid(Instagram,RegexType.Instagram))
        {
            yield return new ValidationResult(
                BasicStatusMessage.IllegalInputData,
                new[] { "instagram" }
            );
        }
    }
}