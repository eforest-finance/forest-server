using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT
{
    public class GetCollectionActivitiesInput : PagedAndSortedMaxCountResultRequestDto
    {
        [CanBeNull] public List<TraitDto> Traits { get; set; }

        [Required] public string CollectionType { get; set; }
        [Required] public string CollectionId { get; set; }

        public List<string> ChainList { get; set; }
        public List<TokenType> SymbolTypeList { get; set; }
        [CanBeNull] public List<int> Type { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            base.Validate(validationContext);

            if (!CollectionType.Equals(CommonConstant.CollectionTypeSeed) &&
                !CollectionType.Equals(CommonConstant.CollectionTypeNFT))
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "collectionType" });
            }
        }
    }
}
    
