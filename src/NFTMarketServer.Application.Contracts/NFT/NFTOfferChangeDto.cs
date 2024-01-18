using Volo.Abp.Application.Dtos;
using System.Collections.Generic;

namespace NFTMarketServer.NFT
{
    public class NFTOfferChangeDto : EntityDto<string>
    {
        public string NftId { get; set; }
        public string ChainId { get; set; }
        public long BlockHeight { get; set; }
    }
    
    public class NFTOfferChangeResultDto
    {
        public List<NFTOfferChangeDto> GetNFTOfferChange{ get; set; }
    }

    public class NFTOfferChangeSignalDto
    {
        public bool hasChanged { get; set; }
    }
}