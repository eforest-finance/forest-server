namespace NFTMarketServer.Grains.Grain.Tree;
[GenerateSerializer]
public class TreeUserActivityRecordGrainDto
{
    [Id(0)]
    public string Id { get; set; }
    [Id(1)]
    public string Address { get; set; }
    [Id(2)]
    public string ActivityId { get; set; }
    [Id(3)]
    public int ClaimCount { get; set; }
}