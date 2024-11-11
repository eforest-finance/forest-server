namespace NFTMarketServer.TreeGame;

public class TreeGameUserInfoDto
{
    public string Id { get; set; }

    //userAccount Address
    public string Address { get; set; }
    
    public string NickName { get; set; }
    
    public decimal Points { get; set; }
    
    public int TreeLevel { get; set; }

    //13 timestamp
    public long CreateTime { get; set; }
    
    public string ParentAddress { get; set; }
    
    public int CurrentWater { get; set; }
    public long WaterUpdateTime { get; set; }

}
