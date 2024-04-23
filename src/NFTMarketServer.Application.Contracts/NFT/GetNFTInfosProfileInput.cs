using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.NFT
{
    public class GetNFTInfosProfileInput : PagedAndMaxCountResultRequestDto
    {
        public List<string> NFTInfoIds { get; set; }
        public string NFTCollectionId { get; set; }
        public string Address { get; set; }
        public string IssueAddress { get; set; }
        public int Status { get; set; }
        public decimal? PriceLow { get; set; }
        public decimal? PriceHigh { get; set; }
        
        public bool IsSeed{ get; set; }
        
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            base.Validate(validationContext);
            
            if (!Address.IsNullOrEmpty() && !Address.MatchesAddress())
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "address" });
            }
            
            if (!IssueAddress.IsNullOrEmpty() && !IssueAddress.MatchesAddress())
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "issueAddress" });
            }

            if (Status != 0 && Status != 1 && Status != 2)
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "status" });
            }

            if (PriceLow != 0 && PriceLow < 0)
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "priceLow" });
            }

            if (PriceHigh != 0 && PriceHigh < 0)
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "priceHigh" });
            }

            if (PriceLow != 0 && PriceHigh != 0 && PriceLow > PriceHigh)
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData,
                    new[] { "priceLow", "priceHigh" });
            }

        }
    }
}
    
