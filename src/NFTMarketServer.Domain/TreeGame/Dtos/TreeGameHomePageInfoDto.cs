using System.Collections.Generic;

namespace NFTMarketServer.Users.Index;

public class TreeGameHomePageInfoDto
{
    public string Id { get; set; }

    //userAccount Address
    public string Address { get; set; }
    
    public string NickName { get; set; }
    
    public decimal TotalPoints { get; set; }
    
    public WaterInfo WaterInfo{ get; set; }
    public TreeInfo TreeInfo{ get; set; }
    public List<PointsDetail> pointsDetails{ get; set; }
}

public class TreeInfo
{
    public TreeLevelInfo Current{ get; set; }
    public TreeLevelInfo Next{ get; set; }
    public long NextLevelCost { get; set; }

}

public class WaterInfo
{
    public long Current{ get; set; }
    public long UpdateTime{ get; set; }
    public long Max { get; set; }
    public long Produce { get; set; }
    public long Frequency { get; set; }
    public long WateringIncome { get; set; }
    public TimeUnit TimeUnit { get; set; }
}

public class PointsDetail
{
    public PointsDetailType Type{ get; set; }
    public long Amount { get; set; }
    public long UpdateTime{ get; set; }
    public long RemainingTime{ get; set; }
    public long ClaimLimit{ get; set; }
    public TimeUnit TimeUnit { get; set; }

}

public class TreeLevelInfo{
    public string Level { get; set; }
    public long Produce { get; set; }
    public int Frequency { get; set; }
    public TimeUnit TimeUnit { get; set; }

}

public enum TimeUnit
{
    SECOND,
    MINUTE,
    HOUR
}

public enum PointsDetailType
{
    NORMALONE,
    NORMALTWO,
    INVITE
}