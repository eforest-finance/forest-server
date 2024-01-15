using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public class NFTImmutableInfoDto: EntityDto<string>
    {
        public string ChainId { get; set; }
        public string TokenHash { get; set; }
        public string Symbol { get; set; }
        public NFTCollectionIndexDto NftCollection { get; set; }
    }
}