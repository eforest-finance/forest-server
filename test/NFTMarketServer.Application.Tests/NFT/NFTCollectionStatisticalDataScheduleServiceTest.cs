using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT;

public class NFTCollectionStatisticalDataScheduleServiceTest : NFTMarketServerApplicationTestBase

{
    private const string ChainId = "AELF";

    private Mock<INFTCollectionProvider> _mockINFTCollectionProvider;
    
    private readonly ILogger<NFTCollectionStatisticalDataScheduleServiceTest> _logger;

    private readonly NFTCollectionStatisticalDataScheduleService _nftCollectionStatisticalDataScheduleService;

    public NFTCollectionStatisticalDataScheduleServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _logger =
            ServiceProvider.GetRequiredService<ILogger<NFTCollectionStatisticalDataScheduleServiceTest>>();

        _nftCollectionStatisticalDataScheduleService =
            GetRequiredService<NFTCollectionStatisticalDataScheduleService>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        _mockINFTCollectionProvider = MockINFTCollectionProvider();
        services.AddSingleton(_mockINFTCollectionProvider.Object);
    }

    private Mock<INFTCollectionProvider> MockINFTCollectionProvider()
    {
        var changes = new IndexerNFTCollectionChanges()
        {
            TotalRecordCount = 2,
            IndexerNftCollectionChanges = new List<IndexerNFTCollectionChange>
            {
                new (ChainId, "seed-0", 1),
                new (ChainId, "seed-1", 2)
            }
        };
        var _mockProvider = new Mock<INFTCollectionProvider>();
        _mockProvider.Setup(ser =>
                ser.GetNFTCollectionChangesByBlockHeightAsync(0, ChainId, 0))
            .Returns(Task.FromResult(changes));
        //The second time，return empty list
        _mockProvider.Setup(ser =>
                ser.GetNFTCollectionChangesByBlockHeightAsync(2, ChainId, 0))
            .Returns(Task.FromResult(new IndexerNFTCollectionChanges { }));

        _mockProvider.Setup(ser =>
                ser.GenerateNFTCollectionExtensionById(ChainId, It.IsAny<string>()))
            .Returns(Task.FromResult(new IndexerNFTCollectionExtension
            {
                ItemTotal = 101,
                OwnerTotal = 202
            }));
        
        _mockProvider.Setup(ser =>
                ser.GetNFTCollectionIndexAsync(It.IsAny<string>()))
            .Returns(Task.FromResult(new IndexerNFTCollection
            {
                Id = "CollectionId",
                ExternalInfoDictionary = new List<IndexerExternalInfoDictionary>
                {
                    new IndexerExternalInfoDictionary
                    {
                        Key = "__nft_image_url",
                        Value = "xxxxxxx"
                    }
                }
            }));
        
        return _mockProvider;
    }


    [Fact]
    public async Task StopwatchTest()
    {
        var stopwatch = Stopwatch.StartNew();
        Thread.Sleep(2000);
        stopwatch.Stop();
        _logger.LogInformation(
            "It took {Elapsed} ms to execute Check Stopwatch for ids {id}.",
            stopwatch.ElapsedMilliseconds, "100001");
    }


    [Fact]
    public async Task SyncIndexerRecordsAsyncTest()
    {
        //Act 
        var maxProcessedBlockHeight =
            await _nftCollectionStatisticalDataScheduleService.SyncIndexerRecordsAsync(ChainId, 0, 1000);

        //Assert
        Assert.Equal(2, maxProcessedBlockHeight);
        
        //check method request times
        _mockINFTCollectionProvider.Verify(provider => provider
            .GetNFTCollectionChangesByBlockHeightAsync(0, ChainId, 0), Times.Exactly(1));
        
        _mockINFTCollectionProvider.Verify(provider => provider
            .GetNFTCollectionChangesByBlockHeightAsync(2, ChainId, 0), Times.Exactly(1));

        _mockINFTCollectionProvider.Verify(provider => provider
            .GenerateNFTCollectionExtensionById(ChainId, It.IsAny<string>()), Times.Exactly(2));
        
    }
}