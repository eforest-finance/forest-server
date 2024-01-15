using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.Market
{
    public interface INFTMarketDataAppService
    {
        Task<ListResultDto<NFTInfoMarketDataDto>> GetMarketDataAsync(GetNFTInfoMarketDataInput input);
    }
}