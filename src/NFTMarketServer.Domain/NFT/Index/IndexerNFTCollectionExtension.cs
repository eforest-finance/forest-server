namespace NFTMarketServer.NFT.Index;

public class IndexerNFTCollectionExtension : IndexerCommonResult<IndexerNFTCollectionExtension>
{
    public long ItemTotal { get; set; }
    
    public long OwnerTotal { get; set; }
}