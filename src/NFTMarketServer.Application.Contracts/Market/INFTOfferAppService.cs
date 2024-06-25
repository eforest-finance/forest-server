using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public interface INFTOfferAppService
    {
        Task<PagedResultDto<NFTOfferDto>> GetNFTOffersAsync(GetNFTOffersInput input);
        
        Task<PagedResultDto<CollectedCollectionOffersDto>> GetCollectedCollectionOffersMadeAsync(GetCollectedCollectionOffersMadeInput input);
        
        Task<PagedResultDto<CollectedCollectionOffersDto>> GetCollectedCollectionReceivedOfferAsync(GetCollectedCollectionReceivedOfferInput input);
    }
}