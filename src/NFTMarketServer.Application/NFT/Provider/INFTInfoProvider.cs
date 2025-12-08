using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;

namespace NFTMarketServer.NFT.Provider;

public interface INFTInfoProvider
{
    public Task<IndexerNFTInfos> GetNFTInfoIndexsAsync(int inputSkipCount,
        int inputMaxResultCount,
        string inputNFTCollectionId,
        string inputSorting,
        decimal inputPriceLow,
        decimal inputPriceHigh,
        int inputStatus,
        string inputAddress,
        string inputIssueAddress,
        List<string> inputNFTInfoIds);

    public Task<IndexerNFTInfo> GetNFTInfoIndexAsync(string id, string address);

    public Task<IndexerNFTInfos> GetNFTInfoIndexsUserProfileAsync(GetNFTInfosProfileInput input);

    public Task<IndexerSymbol> GetNFTCollectionSymbolAsync(string symbol);

    public Task<IndexerSymbol> GetNFTSymbolAsync(string symbol);

    public Task<IndexerNFTInfo> GetNFTSupplyAsync(string nftInfoId);
    
    public Task<IndexerNFTBriefInfos> GetNFTBriefInfosAsync(GetCompositeNFTInfosInput input);
    
    public Task<IndexerNFTOwners> GetNFTOwnersAsync(GetNFTOwnersInput input);
}