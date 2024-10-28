using System.Threading.Tasks;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.TreeGame
{
    public interface ITreeGameService
    {
        Task<TreeGameHomePageInfoDto> GetUserTreeInfo(string address, string nickName);

    }
}