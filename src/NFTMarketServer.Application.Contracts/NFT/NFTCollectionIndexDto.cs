using System.Collections.Generic;
using NFTMarketServer.Users;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public class NFTCollectionIndexDto : EntityDto<string>
    {
        public string ChainId { get; set; }
        public string Symbol { get; set; }
        public string TokenName { get; set; }
        public long TotalSupply { get; set; }
        
        public string CreatorAddress { get; set; }
        public string ProxyOwnerAddress { get; set; }
        public string ProxyIssuerAddress { get; set; }
        public AccountDto Creator { get; set; }
        public bool IsBurnable { get; set; }
        public string IssueChainId { get; set; }
        public List<MetadataDto> Metadata { get; set; }
        public string LogoImage { get; set; }
        public string FeaturedImage { get; set; }
        public string BaseUrl { get; set; }
        public string Description { get; set; }
        public string ExternalLink { get; set; }
        public bool IsOfficial { get; set; }
        public long ItemTotal { get; set; }
        public long OwnerTotal { get; set; }
        public bool IsMainChainCreateNFT { get; set; } = true;
        public bool IsOfficialMark  { get; set; } = false;
    }
}