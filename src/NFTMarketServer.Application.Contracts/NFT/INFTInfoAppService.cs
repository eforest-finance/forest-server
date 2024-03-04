using System.Threading.Tasks;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT
{
    public interface INFTInfoAppService
    {
        Task<long> QueryItemCountForNFTCollectionWithTraitKeyAsync(string key,
            string nftCollectionId);

        Task<long> QueryItemCountForNFTCollectionWithTraitPairAsync(string key, string value,
            string nftCollectionId);

        Task<long> QueryItemCountForNFTCollectionGenerationAsync(string nftCollectionId, int generation);

        Task<NFTInfoNewIndex> QueryFloorPriceNFTForNFTWithTraitPair(string key, string value,
            string nftCollectionId);

        Task<PagedResultDto<NFTInfoIndexDto>> GetNFTInfosAsync(GetNFTInfosInput input);
        
        Task<PagedResultDto<UserProfileNFTInfoIndexDto>> GetNFTInfosForUserProfileAsync(GetNFTInfosProfileInput input);
        
        Task<PagedResultDto<CompositeNFTInfoIndexDto>> GetCompositeNFTInfosAsync(GetCompositeNFTInfosInput input);
        
        Task<NFTInfoIndexDto> GetNFTInfoAsync(GetNFTInfoInput input);

        Task<SymbolInfoDto> GetSymbolInfoAsync(GetSymbolInfoInput input);
        
        Task CreateNFTInfoExtensionAsync(CreateNFTExtensionInput input);
        
        Task AddOrUpdateNftInfoAsync(NFTInfoIndex nftInfo);

        Task<NFTInfoNewIndex> AddOrUpdateNftInfoNewAsync(NFTInfoIndex nftInfo, string nftInfoid, string chainId);

        Task AddOrUpdateNftInfoNewByIdAsync(string nftInfoId, string chainId);
        
        Task<NFTForSaleDto> GetNFTForSaleAsync(GetNFTForSaleInput input);

        Task<NFTOwnerDto> GetNFTOwnersAsync(GetNFTOwnersInput input);
        
    }
}