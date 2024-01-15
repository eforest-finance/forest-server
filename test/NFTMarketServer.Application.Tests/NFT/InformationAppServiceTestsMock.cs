using Moq;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;

namespace NFTMarketServer.NFT;

public sealed partial class InformationAppServiceTests
{
    private INFTInfoProvider GetMockNFTInfoProvider()
    {
        var mockActivityProvider = new Mock<INFTInfoProvider>();

        mockActivityProvider.Setup(m => m.GetNFTSupplyAsync(It.IsAny<string>())).ReturnsAsync(new IndexerNFTInfo()
        {
            Id = "1",
            ChainId = "AELF",
            Symbol = "TEST-1",
            Issuer = "12345",
            TokenName = "TEST NFT",
            Issued = 10,
            TotalSupply = 100
        });

        return mockActivityProvider.Object;
    }
}