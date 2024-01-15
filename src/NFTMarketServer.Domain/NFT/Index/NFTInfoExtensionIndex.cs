using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index
{
    public class NFTInfoExtensionIndex : NFTMarketEntity<string>, IIndexBuild
    {
        [Keyword] public override string Id { get; set; }
        [Keyword] public string ChainId { get; set; }
        [Keyword] public string NFTSymbol { get; set; }
        public string PreviewImage { get; set; }
        public string File { get; set; }
        public string FileExtension { get; set; }
        public string Description { get; set; }
        [Keyword] public string TransactionId { get; set; }
        public string ExternalLink { get; set; }
        
        public string CoverImageUrl { get; set; }
    }
}