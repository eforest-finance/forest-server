using System.Threading.Tasks;
using NFTMarketServer.NFT.Dtos;

namespace NFTMarketServer.NFT.Provider;

public interface ISchrodingerInfoProvider
{
    public Task<SchrodingerSymbolIndexerListDto> GetSchrodingerInfoAsync(GetCatListInput input);
    
}