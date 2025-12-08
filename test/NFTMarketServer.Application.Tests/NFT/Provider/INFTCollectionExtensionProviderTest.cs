using System.Collections.Generic;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Nest;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT.Provider;

public class INFTCollectionExtensionProviderTest : NFTMarketServerApplicationTestBase
{
    private readonly INFTCollectionExtensionProvider _collectionExtensionProvider;
    public INFTCollectionExtensionProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _collectionExtensionProvider = GetRequiredService<INFTCollectionExtensionProvider>();
    }

    [Fact]
    public async void GetNFTCollectionExtensionAsyncTest()
    {
        var result = await _collectionExtensionProvider.GetNFTCollectionExtensionAsync("nftCollectionExtensionIndexId1");
        result.ShouldBeNull();
    }
    
    [Fact]
    public async void GetNFTCollectionExtensionsAsyncTest()
    {   
        var result = await _collectionExtensionProvider.GetNFTCollectionExtensionsAsync(new List<string>(){"nftCollectionExtensionIndexId1","nftCollectionExtensionIndexId2"});
        result.ShouldNotBeNull();
        result.Count.ShouldBe(0);
    }

}