using System.Threading.Tasks;
using NFTMarketServer.Ai;
using NFTMarketServer.NFT;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Platform
{
    public interface IPlatformNFTAppService
    {
        Task<ResultDto<CreatePlatformNFTOutput>> CreatePlatformNFTAsync(CreatePlatformNFTInput input);
    }
}