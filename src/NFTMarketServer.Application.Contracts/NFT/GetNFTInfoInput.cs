using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.NFT
{
    public class GetNFTInfoInput : IValidatableObject
    {
        [Required]
        public string Id { get; set; }
        public string Address { get; set; }
        
        public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            if (!Address.IsNullOrEmpty() && !Address.MatchesAddress())
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "address" });
            }
            
        }
    }
}
    
