namespace NFTMarketServer.Options;

public class TransactionFeeOption
{
    public decimal TransactionFee { get; set; }
    public int Decimals { get; set; } = 8;
}
