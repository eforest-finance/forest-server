using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;
using System.Collections.Generic;

namespace NFTMarketServer.NFT.Index
{
    public class NFTDropExtensionIndex : NFTMarketEntity<string>, IIndexBuild
    {
        [Keyword] public override string Id { get; set; }
        [Keyword] public string DropName { get; set; }
        public string Introduction { get; set; }
        
        public string BannerUrl { get; set; }
        public string LogoUrl { get; set; }
        [Keyword] public string TransactionId { get; set; }
        public decimal ClaimPrice { get; set; }
        public long StartTime { get; set; }
        public long ExpireTime { get; set; }
        public List<SocialMedia> SocialMedia { get; set; }
    }
    
    public class SocialMedia
    {
        public string Type { get; set; }
        public string Link { get; set; }
    }
}