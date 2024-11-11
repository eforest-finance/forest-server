namespace NFTMarketServer.Grains.Grain.Tree;

public class TreeUserActivityRecordGrainDto
{
    public string Id { get; set; }
    public string Address { get; set; }
    public string ActivityId { get; set; }
    public int ClaimCount { get; set; }
}