
namespace NFTMarketServer.TreeGame;

public class TreeLevelUpgradeOutput
{
    public string Address { get; set; }
    
    public long Points { get; set; }
    
    //13 timestamp
    public long OpTime{ get; set; }
    
    public int UpgradeLevel{ get; set; }
    
    public string RequestHash{ get; set; }
}
