using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.NFT
{
    public class GetSeedOwnedSymbols : PagedAndMaxCountResultRequestDto
    {
        [Required]
        public string Address { get; set; }
        public string Symbol { get; set; }
        
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
}