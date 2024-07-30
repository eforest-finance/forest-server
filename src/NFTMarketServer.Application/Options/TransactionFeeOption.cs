using System.Collections.Generic;

namespace NFTMarketServer.Options;

public class TransactionFeeOption
{
    public decimal TransactionFee { get; set; }
    public int Decimals { get; set; } = 8;
    public decimal ForestServiceRate { get; set; }
    public decimal CreatorLoyaltyRate { get; set; }
    public double AIImageFee { get; set; } = 0.1;
    public List<CollectionLoyaltyRate> CollectionLoyaltyRates { get; set; }
}
public class CollectionLoyaltyRate{
    public string Symbol { get; set; }
    public decimal Rate { get; set; }
}
