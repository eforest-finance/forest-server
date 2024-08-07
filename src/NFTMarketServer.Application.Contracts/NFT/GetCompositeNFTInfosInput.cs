using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT
{
    public class GetCompositeNFTInfosInput : PagedAndSortedMaxCountResultRequestDto
    {
        [CanBeNull] public List<string> CollectionIds { get; set; }
        [CanBeNull] public List<string> NFTIdList { get; set; }
        [CanBeNull] public string IssueAddress { get; set; }

        [CanBeNull] public List<int> Generation { get; set; }
        [CanBeNull] public List<TraitDto> Traits { get; set; }

        [Required] public string CollectionType { get; set; }
        [Required] public string CollectionId { get; set; }
        [Required] public override string Sorting { get; set; }

        [Required,DefaultValue(false)]public bool HasListingFlag { get; set; }
        [Required,DefaultValue(false)]public bool HasAuctionFlag { get; set; }
        [Required,DefaultValue(false)]public bool HasOfferFlag { get; set; }
        
        public string SearchParam { get; set; }
        public decimal? PriceLow { get; set; }
        public decimal? PriceHigh { get; set; }
        
        public List<string> ChainList { get; set; }
        public List<TokenType> SymbolTypeList { get; set; }
        public List<string> RarityList { get; set; }
        public bool FuzzySearchSwitch { get; set; }

        public PageFromEnum PageFrom { get; set; }

        public override IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
        {
            base.Validate(validationContext);

            if (!CollectionType.Equals(CommonConstant.CollectionTypeSeed) &&
                !CollectionType.Equals(CommonConstant.CollectionTypeNFT))
            {
                yield return new ValidationResult(BasicStatusMessage.IllegalInputData, new[] { "collectionType" });
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
    
    public enum PageFromEnum
    {
        NFTLIST = 0,
        OTHER = 1
    }
}
    
