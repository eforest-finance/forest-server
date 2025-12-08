using System;
using Moq;
using NFTMarketServer.Tokens;

namespace NFTMarketServer.NFT;

public sealed partial class NftInfoAppServiceTest
{
    private ITokenAppService GetMockTokenAppService()
    {
        var mockTokenAppService = new Mock<ITokenAppService>();

        mockTokenAppService.Setup(m => m.GetTokenMarketDataAsync(It.IsAny<string>(), It.IsAny<DateTime>()))
            .ReturnsAsync(
                new TokenMarketDataDto
                {
                    Price = decimal.One,
                    Timestamp = 2
                }
            );
        return mockTokenAppService.Object;
    }
}