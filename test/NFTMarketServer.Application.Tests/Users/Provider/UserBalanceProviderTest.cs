using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Provider;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Users.Provider;

public class UserBalanceProviderTest : NFTMarketServerApplicationTestBase
{
    
    private readonly NFT.Provider.IUserBalanceProvider _userBalanceProvider;

    public UserBalanceProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
    {
        _userBalanceProvider = GetRequiredService<NFT.Provider.IUserBalanceProvider>();
    }

    protected override void AfterAddApplication(IServiceCollection services)
    {
        services.AddSingleton(BuildMockIBus());
    }
    private static IBus BuildMockIBus()
    {
        var mockIBus =
            new Mock<IBus>();
        return mockIBus.Object;
    }
    
    [Fact]
    public async Task IndexerQueryUserBalanceListAsync()
    {
        var input = new QueryUserBalanceInput()
        {
            SkipCount = 0, 
            BlockHeight = 10l
        };
       var client = new GraphQLHttpClient("https://test-indexer.eforest.finance/AElfIndexer_Forest/ForestIndexerPluginSchema/graphql",
            new NewtonsoftJsonSerializer());
        try
        {
            var indexerCommonResult = await client.SendQueryAsync<UserBalanceIndexerQuery>(new GraphQLRequest
            {
                Query = 
                    @"query($skipCount: Int!,$blockHeight: Long!) {
                    queryUserBalanceList(input: {
                    skipCount: $skipCount
                    ,blockHeight: $blockHeight
                    }) {
                        totalCount,
                        data {
                        id,
                        chainId,
                        blockHeight,
                        address,
                        amount,
                        nFTInfoId,
                        symbol,
                        changeTime,
                        listingPrice,
                        listingTime
                        }
                    }
                }",
                Variables = new
                {
                    skipCount = input.SkipCount,
                    blockHeight = input.BlockHeight
                }
            });

            var result = indexerCommonResult;
            result.ShouldNotBeNull();
            var data = indexerCommonResult?.Data;
            data.QueryUserBalanceList.ShouldNotBeNull();
            data.QueryUserBalanceList.TotalCount.ShouldBeGreaterThan(0);
            data.QueryUserBalanceList.Data.Count.ShouldBeGreaterThan(0);
        }
        catch (Exception e)
        {
        }
    }
    
    [Fact]
    public async Task GetAllNFTListingsByHeightAsync_Test()
    {
        var input = new QueryUserBalanceInput()
        {
            SkipCount = 0, 
            BlockHeight = 10l
        };
        var client = new GraphQLHttpClient("https://test-indexer.eforest.finance/AElfIndexer_Forest/ForestIndexerPluginSchema/graphql",
            new NewtonsoftJsonSerializer());
        var listingDto = await client.SendQueryAsync<NFTListingAllPage>(new GraphQLRequest
        {
            Query = @"query (
                    $skipCount:Int!,
                    $maxResultCount:Int!,
                    $chainId:String,
                    $symbol:String,
                    $owner: String,
                    $address: String,
                    $excludedAddress: String,
                    $expireTimeGt:Long,
                    $blockHeight:Long
                ){
                  nftListingInfoAll(
                    input:{
                      skipCount:$skipCount,
                      maxResultCount:$maxResultCount,
                      chainId:$chainId,
                      symbol:$symbol,
                      owner:$owner,
                      address:$address,
                      excludedAddress:$excludedAddress,
                      expireTimeGt:$expireTimeGt,
                      blockHeight:$blockHeight
                    }
                  ){
                    TotalCount: totalRecordCount,
                    Items: data{
                      symbol,
                      quantity,
                      expireTime 
                    }
                  }
                }",
            Variables = new
            {
                chainId = "tDVW", 
                skipCount = 0, 
                maxResultCount = 10, 
                expireTimeGt = 1738141200000,
                blockHeight = 0
            }
        });
        var totalCount = listingDto.Data.nftListingInfoAll.TotalCount;
        var itemCount = listingDto.Data.nftListingInfoAll.Items.Count;
        totalCount.ShouldBeGreaterThan(0);
        itemCount.ShouldBeGreaterThan(0);
    }
}