using System.Threading.Tasks;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.TreeGame
{
    public interface ITreeGameService
    {
        Task<TreeGameHomePageInfoDto> GetUserTreeInfoAsync(string address, string nickName, bool needStorage);
        
        Task<TreeGameHomePageInfoDto> WateringTreeAsync(string address, int count);

        Task<TreeLevelUpgradeOutput> UpgradeTreeLevelAsync(string address, int nextLevel);

        Task<TreePointsClaimOutput> ClaimAsync(string address, PointsDetailType pointsDetailType);
        
        Task<TreePointsClaimOutput> PointsConvertAsync(string address, string activityId);

    }
}