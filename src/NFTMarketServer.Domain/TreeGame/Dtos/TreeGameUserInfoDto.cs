using Orleans;

namespace NFTMarketServer.TreeGame;
[GenerateSerializer]
public class TreeGameUserInfoDto
{
    [Id(0)]
    public string Id { get; set; }

    //userAccount Address
    [Id(1)]
    public string Address { get; set; }

    [Id(2)]
    public string NickName { get; set; }

    [Id(3)]
    public decimal Points { get; set; }

    [Id(4)]
    public int TreeLevel { get; set; }

    //13 timestamp
    [Id(5)]
    public long CreateTime { get; set; }

    [Id(6)]
    public string ParentAddress { get; set; }

    [Id(7)]
    public int CurrentWater { get; set; }
    [Id(8)]
    public long WaterUpdateTime { get; set; }

}
