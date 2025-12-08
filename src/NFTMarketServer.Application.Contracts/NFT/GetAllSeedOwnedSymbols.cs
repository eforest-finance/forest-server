
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.NFT
{
    public class GetAllSeedOwnedSymbols : PagedAndMaxCountResultRequestDto
    {
        [Required]
        public List<string> AddressList { get; set; }
        public string SeedOwnedSymbol { get; set; }
        
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            base.Validate(validationContext);

            if (!AddressList.IsNullOrEmpty())
            {
                if (AddressList.Any(item => !item.IsNullOrEmpty() && !item.MatchesAddress()))
                {
                    yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "address" });
                }
            }
        }
    }
}