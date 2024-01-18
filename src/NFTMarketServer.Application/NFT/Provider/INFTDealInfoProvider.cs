using System.Threading.Tasks;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTDealInfoProvider
{
    public Task<IndexerNFTDealInfos> GetDealInfosAsync(GetNftDealInfoDto dto);
}