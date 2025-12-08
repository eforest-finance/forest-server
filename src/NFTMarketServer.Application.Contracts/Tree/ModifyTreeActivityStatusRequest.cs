namespace NFTMarketServer.Tree;

public class ModifyTreeActivityStatusRequest
{
    public string Id { get; set; }
    public TreeActivityStatus TreeActivityStatus { get; set; }
}