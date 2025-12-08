using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTDropInfoProvider
{
    public Task<NFTDropInfoIndexList> GetNFTDropInfoIndexListAsync(GetNFTDropListInput input);
    
    public Task<NFTDropInfoIndex> GetNFTDropInfoIndexAsync(string dropId);

    public Task<NFTDropClaimIndex> GetNFTDropClaimIndexAsync(string dropId, string address);
    
    public Task<NFTDropInfoIndexList> GetExpireNFTDropListAsync();
}