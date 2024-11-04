
namespace NFTMarketServer.TreeGame;

public class TreePointsClaimOutput
{
    public string Address { get; set; }
    
    public PointsDetailType PointsDetailType{ get; set; }
    public decimal Points { get; set; }
    
    //13 timestamp
    public long OpTime{ get; set; }
    public string RequestHash{ get; set; }
}
