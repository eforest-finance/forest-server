using System;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace NFTMarketServer.Users.Provider;

public class TreeGamePointsRecordProviderTest : NFTMarketServerApplicationTestBase
{
    
    private readonly NFT.Provider.IUserBalanceProvider _userBalanceProvider;

    public TreeGamePointsRecordProviderTest(ITestOutputHelper testOutputHelper) : base(testOutputHelper)
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
       var client = new GraphQLHttpClient("https://test-indexer-api.aefinder.io/api/app/graphql/forest",
            new NewtonsoftJsonSerializer());
       var totalCount = 0;
       var itemCount = 0;
       try
       {
           var graphQlResponse = await client.SendQueryAsync<IndexerTreePointsRecordPage>(new GraphQLRequest
           {
               Query = @"
			    query($startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String!) {
                    data:getSyncTreePointsRecords(dto:{startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId}){
                        totalRecordCount,
                        treePointsChangeRecordList:data{
                             id,
                             address,
                             totalPoints,
                             points,
                             opType,
                             opTime,
                             pointsType,
                             activityId,
                             treeLevel,
                             chainId,
                             blockHeight      
                            
                                                             
                         }
                    }
                }",
               Variables = new
               {
                   startBlockHeight = 300,
                   endBlockHeight = 0,
                   chainId = "tDVW"
               }
           });
           totalCount = (int)graphQlResponse.Data.Data.TotalRecordCount;
           itemCount = graphQlResponse.Data.Data.TreePointsChangeRecordList.Count;

           foreach (var item in graphQlResponse.Data.Data.TreePointsChangeRecordList)
           {
               var type = item.PointsType;
           }
          
         
       }
       catch (Exception e)
       {
           var err = e.Message;
           totalCount = 0;
           itemCount = 0;
       }
       totalCount.ShouldBeGreaterThan(0);
       itemCount.ShouldBeGreaterThan(0);

      

        /*
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
            */
    }
           
}