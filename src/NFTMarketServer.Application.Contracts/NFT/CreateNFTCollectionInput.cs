using System.Collections.Generic;

namespace NFTMarketServer.NFT
{
    public class CreateNFTCollectionInput:InputBase
    {
        public string Symbol { get; set; }
        public string CollectionName { get; set; }
        public long TotalSupply { get; set; }
        public string Creator { get; set; }
        public bool IsBurnable { get; set; }
        public int IssueChainId { get; set; }
        public List<MetadataDto> Metadata { get; set; }
        public string BaseUri { get; set; }
        public bool IsTokenIdReuse { get; set; }
        public string NFTType { get; set; }
        public string TransactionId { get; set; }
    }
}