using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT
{
    public class GetCollectedCollectionActivitiesInput : PagedAndSortedMaxCountResultRequestDto
    {
        [CanBeNull] public List<TraitDto> Traits { get; set; }
        [Required] public string CollectionId { get; set; }
        public List<string> ChainList { get; set; }
        public List<TokenType> SymbolTypeList { get; set; }
        [CanBeNull] public List<int> Type { get; set; }
        public string Address { get; set; }
    }
}
    
