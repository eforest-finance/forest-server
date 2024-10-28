using System.Threading.Tasks;
using NFTMarketServer.Users.Index;

namespace NFTMarketServer.Users
{
    public interface ITreeGameService
    {
        Task<TreeGameHomePageInfoDto> GetUserTreeInfo(string address, string nickName);

    }
}