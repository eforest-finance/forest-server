namespace NFTMarketServer.NFT.Index;

public class IndexerNFTCollectionTrade : IndexerCommonResult<IndexerNFTCollectionTrade>
{
    public decimal VolumeTotal { get; set; } = 0;
    public decimal FloorPrice { get; set; } = -1;
    public long SalesTotal { get; set; } = 0;
}