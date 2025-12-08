using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public class GetCollectedCollectionListingsInput: PagedAndSortedResultRequestDto
    {
        [CanBeNull] public string SearchParam { get; set; }
        [CanBeNull] public List<string> CollectionIdList { get; set; }
        [CanBeNull] public List<string> ChainList { get; set; }
        public string Address { get; set; }
        
        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            base.Validate(validationContext);

            if (!Address.IsNullOrEmpty() && !Address.MatchesAddress())
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "address" });
            }

        }
    }
}