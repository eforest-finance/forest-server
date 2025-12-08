namespace NFTMarketServer.NFT.Index;

public class IndexerNFTBalanceInfo : IndexerCommonResult<IndexerNFTBalanceInfo>
{
    public string Owner { get; set; }

    public long OwnerCount { get; set; }
}