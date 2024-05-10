using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using Castle.Core.Internal;
using NFTMarketServer.Basic;

namespace NFTMarketServer.NFT;

public class SearchCollectionsFloorPriceInput : PagedAndSortedMaxCountResultRequestDto
{
    public string ChainId { get; set; }
    public List<string> CollectionSymbolList { get; set; }
    
    public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        base.Validate(validationContext);
        if (ChainId.IsNullOrEmpty())
        {
            yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "chainId" });
        }
        if (CollectionSymbolList.IsNullOrEmpty())
        {
            yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "collectionSymbolList" });
        }
    }
}