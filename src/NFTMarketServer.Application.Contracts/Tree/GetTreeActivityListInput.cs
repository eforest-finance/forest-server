namespace NFTMarketServer.Tree;

public class GetTreeActivityListInput
{
    public int SkipCount { get; set; }
    public int MaxResultCount { get; set; }
    public string Address{ get; set; }
}