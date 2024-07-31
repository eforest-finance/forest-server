using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public class GetNFTListingsInput : PagedAndSortedResultRequestDto, IValidatableObject
    {
        [Required] public string ChainId { get; set; }
        public string Symbol { get; set; }
        
        public string Address { get; set; }
        
        public string ExcludedAddress { get; set; }
        public long BlockHeight { get; set; }

        
        public IEnumerable<ValidationResult> Validate(
            ValidationContext validationContext)
        {
            if (ChainId.IsNullOrEmpty() || !ChainId.MatchesChainId())
                yield return new ValidationResult($"ChainId invalid.");
            if (Symbol.IsNullOrEmpty() || !Symbol.MatchesNftSymbol())
                yield return new ValidationResult($"Symbol invalid.");
        }
        
    }
}