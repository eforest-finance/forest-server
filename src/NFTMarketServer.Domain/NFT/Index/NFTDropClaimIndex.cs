namespace NFTMarketServer.NFT.Index;

public class NFTDropClaimIndex : IndexerCommonResult<NFTDropClaimIndex>
{
    public string Address { get; set; }
    public string DropId { get; set; }
    public long ClaimLimit { get; set; }
    public long ClaimAmount { get; set; }
}