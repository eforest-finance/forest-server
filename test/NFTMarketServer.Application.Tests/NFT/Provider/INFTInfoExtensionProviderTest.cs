using System.Collections.Generic;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT.Provider;

public class INFTInfoExtensionProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTInfoExtensionProvider _nftInfoExtensionProvider;
    public INFTInfoExtensionProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _nftInfoExtensionProvider = GetRequiredService<INFTInfoExtensionProvider>();
    }
    
    [Fact]
    public async void GetNFTCollectionExtensionAsyncTest()
    {
        var result = await _nftInfoExtensionProvider.GetNFTInfoExtensionsAsync(new List<string>(){"nftCollectionExtensionIndexId1","nftCollectionExtensionIndexId2"});
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }
}