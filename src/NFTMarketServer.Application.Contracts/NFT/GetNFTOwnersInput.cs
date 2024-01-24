using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using NFTMarketServer.Basic;
using NFTMarketServer.Helper;

namespace NFTMarketServer.NFT
{
    public class GetNFTOwnersInput : PagedAndSortedMaxCountResultRequestDto
    {
        public string Id { get; set; }
        public string ChainId { get; set; }
    }
}