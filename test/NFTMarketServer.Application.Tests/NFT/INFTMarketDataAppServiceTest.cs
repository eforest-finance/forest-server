using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.Common;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.NFT
{
    public class INFTMarketDataAppServiceTest : NFTMarketServerApplicationTestBase
    {
        private readonly INFTMarketDataAppService _nftMarketDataAppService;
        
        public INFTMarketDataAppServiceTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
        {
            _nftMarketDataAppService = GetRequiredService<INFTMarketDataAppService>();
        }

        protected override void AfterAddApplication(IServiceCollection services)
        {
            services.AddSingleton(BuildMockIGraphQLHelper());
        }

        [Fact]
        public async Task GetMarketDataAsync_ShouldBe0()
        {
            var input = new GetNFTInfoMarketDataInput
            {
                NFTInfoId = "tDVW-JINMINGTUESSTT-1000"
            };

            var res = await _nftMarketDataAppService.GetMarketDataAsync(input);
            //TestOutputHelper.WriteLine(JsonConvert.SerializeObject(res));
            res.Items.Count.ShouldBe(1);
        }

        private static IGraphQLHelper BuildMockIGraphQLHelper()
        {
            var mockIGraphQLHelper = new Mock<IGraphQLHelper>();
            
            mockIGraphQLHelper.Setup(cals => cals.QueryAsync<IndexerNFTInfoMarketDatas>(It.IsAny<GraphQLRequest>()))
                .ReturnsAsync(new IndexerNFTInfoMarketDatas
                {
                    Data = new IndexerNFTInfoMarketDatas
                    {
                        TotalRecordCount = 0,
                        indexerNftInfoMarketDatas = new List<IndexerNFTInfoMarketData>
                        {
                            new IndexerNFTInfoMarketData
                            {
                                Price = 1300000000,
                                Timestamp = 1689638400000,
                                Data = null
                            }
                        }
                    }
                });
            return mockIGraphQLHelper.Object;
        }
    }
}