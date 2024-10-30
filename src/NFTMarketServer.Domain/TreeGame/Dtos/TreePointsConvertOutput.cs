
namespace NFTMarketServer.TreeGame;

public class TreePointsConvertOutput
{
    public string Address { get; set; }
    
    public string ActivityId { get; set; }
    public long Points { get; set; }
    
    //13 timestamp
    public long OpTime{ get; set; }
    
    public string RequestHash{ get; set; }
}
