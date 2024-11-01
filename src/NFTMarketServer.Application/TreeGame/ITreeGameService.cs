using System.Threading.Tasks;
using NFTMarketServer.Tree;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.TreeGame
{
    public interface ITreeGameService
    {
        Task<TreeGameHomePageInfoDto> GetUserTreeInfoAsync(string address, string nickName, bool needStorage);
        
        Task<TreeGameHomePageInfoDto> WateringTreeAsync(TreeWateringRequest input);

        Task<TreeLevelUpgradeOutput> UpgradeTreeLevelAsync(TreeLevelUpdateRequest request);

        Task<TreePointsClaimOutput> ClaimAsync(TreePointsClaimRequest request);
        
        Task<TreePointsConvertOutput> PointsConvertAsync(TreePointsConvertRequest request);

    }
}