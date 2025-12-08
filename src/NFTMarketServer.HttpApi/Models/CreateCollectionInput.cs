using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;

namespace NFTMarketServer.Models
{
    public class CreateCollectionInput : IValidatableObject
    {
        [Required]public string FromChainId { get; set; }
        public string TransactionId { get; set; }
        [MaxLength(1000)]public string Description { get; set; }
        [MaxLength(100)] public string ExternalLink { get; set; }
        [Required] public string Symbol { get; set; }
        [Required]
        [MinLength(1)]
        public string LogoImage { get; set; }
        public string FeaturedImage { get; set; }
        public string TokenName { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (! RegexHelper.IsValid(ExternalLink,RegexType.HttpAddress))
            {
                yield return new ValidationResult(
                    BasicStatusMessage.IllegalInputData,
                    new[] { "externalLink" }
                );
            }
        }
    }
}