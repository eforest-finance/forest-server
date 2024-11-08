namespace NFTMarketServer.Grains.State.NFTInfo;

public class TreeUserActivityRecordState
{
    public string Id { get; set; }
    public string Address { get; set; }
    public string ActivityId { get; set; }
    public int ClaimCount { get; set; }
}