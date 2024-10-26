namespace NFTMarketServer.Tree;

public class CreateTreeActivicyRequest
{
    public string ImageUrl { get; set; }
    public string ActivityName { get; set; }
    public string ActivityDesc { get; set; }
    public string RewardName { get; set; }
    public string Condition { get; set; }
    public decimal TotalReward { get; set; }
    public decimal LeftReward { get; set; }
    public decimal RewardLogo { get; set; }
    public RewardType RewardType { get; set; }
    public long BeginDateTimeMilliseconds { get; set; }
}