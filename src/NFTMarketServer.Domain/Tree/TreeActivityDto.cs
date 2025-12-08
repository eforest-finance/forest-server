using System;

namespace NFTMarketServer.Tree;

public class TreeActivityDto
{
    public string Id { get; set; }
    public string OriginId { get; set; }
    public string ImageUrl { get; set; }
    public string ActivityName { get; set; }
    public string ActivityDesc { get; set; }
    public string RewardName { get; set; }
    //bonus pool maybe Token or NFT
    public decimal TotalReward { get; set; }
    // remaining bonus
    public decimal LeftReward { get; set; }
    //The number of bonuses that can be claimed at once
    public decimal RedeemRewardOnce{ get; set; }
    public RedeemType RedeemType{ get; set; }
    //need MinPoints Participate in activities
    public long MinPoints{ get; set; }
    //cost Points Participate in activities
    public long CostPoints{ get; set; }
    
    public string RewardLogo { get; set; }
    public RewardType RewardType { get; set; }
    public TreeActivityStatus TreeActivityStatus { get; set; }
    public bool HideFlag { get; set; }
    public DateTime BeginDateTime { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastModifyTime { get; set; }
    public int Frequency{ get; set; }
    public bool CanClaim { get; set; } = true;

}