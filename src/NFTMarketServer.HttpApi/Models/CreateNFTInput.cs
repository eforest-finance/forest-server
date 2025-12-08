using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;

namespace NFTMarketServer.Models
{
    public class CreateNFTInput : IValidatableObject
    {
        [Required]public string ChainId { get; set; }
        public string TransactionId { get; set; }
        
        [Required]public string Symbol { get; set; }
        [MaxLength(1000)] public string Description { get; set; }
        [MaxLength(100)] public string ExternalLink { get; set; } 
        public string PreviewImage { get; set; }
        public string File { get; set; }
        public string CoverImageUrl { get; set; }
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

    public class BatchCreateNFTInput
    {
        public List<CreateNFTInput> NFTList { get; set; }
    }
}