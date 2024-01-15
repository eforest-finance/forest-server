using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Grains.Grain.NFTInfo;
using NFTMarketServer.NFT.Index;
using Orleans;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT.Provider;

public class INFTCollectionProviderAdapterTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTCollectionProviderAdapter _nftCollectionProviderAdapter;
    private readonly IClusterClient _clusterClient;

    public INFTCollectionProviderAdapterTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftCollectionProviderAdapter = GetRequiredService<INFTCollectionProviderAdapter>();
        _clusterClient = GetRequiredService<IClusterClient>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildINFTCollectionProvider());
    }

    private INFTCollectionProvider BuildINFTCollectionProvider()
    {
        var mockCollectionProvider = new Mock<INFTCollectionProvider>();

        mockCollectionProvider.Setup(self => self.GetNFTCollectionIndexAsync(It.IsAny<string>()))
            .ReturnsAsync(new IndexerNFTCollection
            {
                Id = "tDVV-test-0",
                ChainId = "tDVV",
                LogoImage = "https://forest-dev.s3.ap-northeast-1.amazonaws.com/1701763706398-banner.png",
                TokenName = "test for ut",
                CreateTime = DateTime.UtcNow,
                Symbol = "test-0"
            });

        return mockCollectionProvider.Object;
    }


    [Fact]
    public async Task AddOrUpdateNftCollectionExtensionTest()
    {
        // Arrange
        var id = "tDVV-test-0";
        var dto = new NFTCollectionExtensionDto()
        {
            Id = id
        };

        // Act
        await _nftCollectionProviderAdapter.AddOrUpdateNftCollectionExtensionAsync(dto);

        // Assert
        var nftCollectionExtensionGrain = _clusterClient.GetGrain<INFTCollectionExtensionGrain>(dto.Id);
        var grainDto = (await nftCollectionExtensionGrain.GetAsync()).Data;
        Assert.NotNull(grainDto);
        Assert.Equal(id, grainDto.Id);
        Assert.Equal("test-0", grainDto.NFTSymbol);
        Assert.Equal(-1, grainDto.FloorPrice);
    }
}