using System.Threading.Tasks;

namespace NFTMarketServer.Chains
{
    public interface IChainAppService
    {
        Task<string[]> GetListAsync();
        
        Task<string> GetChainIdAsync(int index);
        
    }
}