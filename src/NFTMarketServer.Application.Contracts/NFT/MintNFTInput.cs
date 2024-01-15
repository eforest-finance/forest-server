using System;
using System.Collections.Generic;

namespace NFTMarketServer.NFT
{
    public class MintNFTInput:InputBase
    {
        public string Symbol { get; set; }
        public string CollectionName { get; set; }
        public long TokenId { get; set; }
        public string Creator { get; set; }
        public string Minter { get; set; }
        public List<MetadataDto> Metadata { get; set; } = new();
        public string Owner { get; set; }
        public string Uri { get; set; }
        public string BaseUri { get; set; }
        public string Alias { get; set; }
        public string NFTType { get; set; }
        public long Quantity { get; set; }
        public long TotalQuantity { get; set; }
        public string TokenHash { get; set; }
        public string TransactionId { get; set; }
        public bool IsOfficial { get; set; }
        public DateTime MintTime { get; set; }
    }
}