namespace NFTMarketServer.Market;

public class GetNFTListingsDto
{
    public string ChainId { get; set; }
    
    public string Symbol { get; set; }
    
    public string Address { get; set; }
    
    public string ExcludedAddress { get; set; }
    
    public int SkipCount { get; set; }
    
    public int MaxResultCount { get; set; }
}