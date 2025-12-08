using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.NFT
{
    public class GetNFTCollectionsInput : PagedAndMaxCountResultRequestDto
    {
        public string Address { get; set; }
        
        public List<string> AddressList { get; set; }
        
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

            if (!Address.IsNullOrEmpty() && !Address.MatchesAddress())
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "address" });
            }

        }
    }
}