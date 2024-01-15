namespace NFTMarketServer.Grains.Grain.Options;

public class ContractOption
{
    public int RetryTimes { get; set; } = 5;

    public int RetryDelay { get; set; } = 1000;
}