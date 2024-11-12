using System.Collections.Generic;

namespace NFTMarketServer.TreeGame;
public class TreeGameConstants
{
    public static readonly List<TreeLevelInfo> TreeLevels = new List<TreeLevelInfo>()
    {
        new TreeLevelInfo
        {
            Level = 1,
            LevelTitle = "LV1",
            Produce = 190,
            Frequency = 10,
            TimeUnit = TimeUnit.MINUTE,
            MinPoints = 0
        },
        new TreeLevelInfo
        {
            Level = 2,
            LevelTitle = "LV2",
            Produce = 20,
            Frequency = 720,
            TimeUnit = TimeUnit.MINUTE,
            MinPoints = 100
        },
        new TreeLevelInfo
        {
            Level = 3,
            LevelTitle = "LV3",
            Produce = 30,
            Frequency = 720,
            TimeUnit = TimeUnit.MINUTE,
            MinPoints = 200
        },
        new TreeLevelInfo
        {
            Level = 4,
            LevelTitle = "LV4",
            Produce = 40,
            Frequency = 720,
            TimeUnit = TimeUnit.MINUTE,
            MinPoints = 300
        },
        new TreeLevelInfo
        {
            Level = 5,
            LevelTitle = "LV5",
            Produce = 50,
            Frequency = 720,
            TimeUnit = TimeUnit.MINUTE,
            MinPoints = 400
        },
        new TreeLevelInfo
        {
            Level = 6,
            LevelTitle = "LV6",
            Produce = 60,
            Frequency = 720,
            TimeUnit = TimeUnit.MINUTE,
            MinPoints = 500
        }
    };

    public static readonly WaterInfoConfig WaterInfoConfig = new WaterInfoConfig()
    {
        Max = 60,
        Produce = 1,
        Frequency = 10,
        TimeUnit = TimeUnit.MINUTE,
        WateringIncome = 2
    };

    public static readonly List<PointsDetailConfig> PointsDetailConfig = new List<PointsDetailConfig>()
    {
        new PointsDetailConfig()
        {
            Type = PointsDetailType.NORMALONE,
            ClaimLimit = 0,
            TimeUnit = TimeUnit.MINUTE
        },
        new PointsDetailConfig()
        {
            Type = PointsDetailType.NORMALTWO,
            ClaimLimit = 0,
            TimeUnit = TimeUnit.MINUTE
        },
        new PointsDetailConfig()
        {
            Type = PointsDetailType.INVITE,
            ClaimLimit = 100,
            TimeUnit = TimeUnit.MINUTE
        }
    };

    public const string HashVerifyKey = "1a2b3c";
    public const string TreeGameInviteType = "treegame";
    public const long TreeGameInviteReward = 100;
    public const double RewardProportion = 0.05;
    public const int DefaultRewardDecimal = 8;

    public static readonly List<Reward> RewardsConfig = new List<Reward>()
    {
        new Reward()
        {
            Symbol = "ELF",
            Decimals = 8
        },
        new Reward()
        {
            Symbol = "SGR-0",
            Decimals = 8
        },
        new Reward()
        {
            Symbol = "USDT",
            Decimals = 6
        }
    };

}
public class Reward
{
    public string Symbol{ get; set; }
    public int Decimals{ get; set; }
}

