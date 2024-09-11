using System.Threading.Tasks;

namespace NFTMarketServer.Platform
{
    public interface IPlatformNFTAppService
    {
        Task<CreatePlatformNFTOutput> CreatePlatformNFTV1Async(CreatePlatformNFTInput input);
        
        Task<CreatePlatformNFTOutput> CreatePlatformNFTAsync(CreatePlatformNFTInput input);
        Task<CreatePlatformNFTRecordInfo> GetPlatformNFTInfoAsync(string address);

    }
}