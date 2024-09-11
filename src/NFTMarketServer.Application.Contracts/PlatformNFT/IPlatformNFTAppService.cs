using System.Threading.Tasks;
using NFTMarketServer.Ai;
using NFTMarketServer.NFT;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Platform
{
    public interface IPlatformNFTAppService
    {
        Task<CreatePlatformNFTOutput> CreatePlatformNFTV1Async(CreatePlatformNFTInput input);
        
        Task<CreatePlatformNFTOutput> CreatePlatformNFTAsync(CreatePlatformNFTInput input);

    }
}