using System.Threading.Tasks;
using NFTMarketServer.Tree;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.TreeGame
{
    public interface ITreeGameService
    {
        Task<TreeGameHomePageInfoDto> GetUserTreeInfoAsync(string address, string nickName, bool needStorage);
        
        Task<TreeGameHomePageInfoDto> WateringTreeAsync(TreeWateringRequest input);

        Task<TreeLevelUpgradeOutput> UpgradeTreeLevelAsync(string address, int nextLevel);

        Task<TreePointsClaimOutput> ClaimAsync(string address, PointsDetailType pointsDetailType);
        
        Task<TreePointsConvertOutput> PointsConvertAsync(string address, string activityId);

    }
}