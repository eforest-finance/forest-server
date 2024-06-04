namespace NFTMarketServer.Grains.Grain.ApplicationHandler;

public class ChainInfo
{
    public string ChainId { get; set; }
    public string BaseUrl { get; set; }
    public string TokenContractAddress { get; set; }
    public string CrossChainContractAddress { get; set; }
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
    public string ProxyAccountAddress { get; set; }
    public string SymbolRegistrarContractAddress { get; set; }
    public string AuctionContractAddress { get; set; }
    public string ReceivingAddress { get; set; }
    
    public string TokenAdapterContractAddress{ get; set; }
    
    public string ForestContractAddress { get; set; }
    
    public string CaContractAddress { get; set; }
} 