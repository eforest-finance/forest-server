using System.Threading.Tasks;

namespace NFTMarketServer.Platform
{
    public interface IPlatformNFTAppService
    {
        Task<CreatePlatformNFTOutput> CreatePlatformNFTAsync(CreatePlatformNFTInput input);
        Task<CreatePlatformNFTRecordInfo> GetPlatformNFTInfoAsync(string address);

    }
}