using System.Threading.Tasks;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public interface INFTInfoAppService
    {
        Task<PagedResultDto<UserProfileNFTInfoIndexDto>> GetNFTInfosForUserProfileAsync(GetNFTInfosProfileInput input);
        
        Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetCompositeNFTInfosAsync(GetCompositeNFTInfosInput input);
        
        Task<PagedResultDto<CollectionActivitiesDto>> GetCollectionActivitiesAsync(GetCollectionActivitiesInput input);
        
        Task<NFTInfoIndexDto> GetNFTInfoAsync(GetNFTInfoInput input);

        Task<SymbolInfoDto> GetSymbolInfoAsync(GetSymbolInfoInput input);
        
        Task CreateNFTInfoExtensionAsync(CreateNFTExtensionInput input);
        
        Task AddOrUpdateNftInfoAsync(NFTInfoIndex nftInfo);

        Task AddOrUpdateNftInfoNewAsync(NFTInfoIndex nftInfo, string nftInfoid, string chainId);

        Task AddOrUpdateNftInfoNewByIdAsync(string nftInfoId, string chainId);
        
        Task<NFTForSaleDto> GetNFTForSaleAsync(GetNFTForSaleInput input);

        Task<NFTOwnerDto> GetNFTOwnersAsync(GetNFTOwnersInput input);
        
        Task<PagedResultDto<NFTActivityDto>> GetActivityListAsync(GetActivitiesInput input);

        
    }
}