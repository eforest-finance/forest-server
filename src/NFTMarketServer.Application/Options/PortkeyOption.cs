namespace NFTMarketServer.Options;

public class PortkeyOption
{
    public string Name { get; set; }
    public string CallBackUrl { get; set; }
    public string CreateOrderUrl { get; set; }
    public string SearchOrderUrl { get; set; }
    public string NotifyReleaseUrl { get; set; }
    public string PublicKey { get; set; }
    public string PrivateKey { get; set; }
}