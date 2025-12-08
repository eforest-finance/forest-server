using System.Collections.Generic;

namespace NFTMarketServer.TreeGame;

public class TreeGameHomePageInfoDto
{
    public string Id { get; set; }

    //userAccount Address
    public string Address { get; set; }
    
    public string NickName { get; set; }
    
    public decimal TotalPoints { get; set; }
    
    public WaterInfo WaterInfo{ get; set; }
    public TreeInfo TreeInfo{ get; set; }
    public List<PointsDetail> PointsDetails{ get; set; }
}

public class TreeInfo
{
    public TreeLevelInfo Current{ get; set; }
    public TreeLevelInfo Next{ get; set; }
    public long NextLevelCost { get; set; }

}

public class WaterInfo
{
    public int Current{ get; set; }
    public long UpdateTime{ get; set; }
    public long Max { get; set; }
    public long Produce { get; set; }
    public long Frequency { get; set; }
    public long WateringIncome { get; set; }
    public TimeUnit TimeUnit { get; set; }
}

public class WaterInfoConfig
{
    public int Max { get; set; }
    public long Produce { get; set; }
    public long Frequency { get; set; }
    public long WateringIncome { get; set; }
    public TimeUnit TimeUnit { get; set; }
}

public class PointsDetail
{
    public string Id { get; set; }
    public string Address { get; set; }
    public PointsDetailType Type{ get; set; }
    public long Amount { get; set; }
    public long UpdateTime{ get; set; }
    public long RemainingTime{ get; set; }
    public long ClaimLimit{ get; set; }
    public TimeUnit TimeUnit { get; set; }

}

public class TreeLevelInfo{
    public int Level{ get; set; }
    public string LevelTitle { get; set; }
    public long Produce { get; set; }
    public int Frequency { get; set; }
    public TimeUnit TimeUnit { get; set; }
    
    public int MinPoints{ get; set; }

}

public class TreeLevelConfig
{
    public int Level{ get; set; }
    public string LevelTitle { get; set; }
    public long Produce { get; set; }
    public int Frequency { get; set; }
    public TimeUnit TimeUnit { get; set; }
}

public class PointsDetailConfig
{
    public PointsDetailType Type{ get; set; }
    public long ClaimLimit{ get; set; }
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