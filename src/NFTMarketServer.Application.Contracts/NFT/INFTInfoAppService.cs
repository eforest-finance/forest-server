using System.Threading.Tasks;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public interface INFTInfoAppService
    {
        Task<PagedResultDto<NFTInfoIndexDto>> GetNFTInfosAsync(GetNFTInfosInput input);
        
        Task<PagedResultDto<UserProfileNFTInfoIndexDto>> GetNFTInfosForUserProfileAsync(GetNFTInfosProfileInput input);
        
        Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetCompositeNFTInfosAsync(GetCompositeNFTInfosInput input);
        
        Task<NFTInfoIndexDto> GetNFTInfoAsync(GetNFTInfoInput input);

        Task<SymbolInfoDto> GetSymbolInfoAsync(GetSymbolInfoInput input);
        
        Task CreateNFTInfoExtensionAsync(CreateNFTExtensionInput input);
        
        Task AddOrUpdateNftInfoAsync(NFTInfoIndex nftInfo);
    }
}