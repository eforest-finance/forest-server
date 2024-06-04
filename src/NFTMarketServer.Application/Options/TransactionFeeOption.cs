namespace NFTMarketServer.Options;

public class TransactionFeeOption
{
    public decimal TransactionFee { get; set; }
    public int Decimals { get; set; } = 8;
    public decimal ForestServiceRate { get; set; }
    public decimal CreatorLoyaltyRate { get; set; }
    public double AIImageFee { get; set; } = 0.1;

}
