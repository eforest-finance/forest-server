using NFTMarketServer.TreeGame;

namespace NFTMarketServer.Tree;

public class TreePointsClaimRequest
{
    public string Address { get; set; }
    public PointsDetailType PointsDetailType { get; set; }

}