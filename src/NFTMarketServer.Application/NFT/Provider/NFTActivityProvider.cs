using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface INFTActivityProvider
{
    public Task<NFTActivityIndex> GetNFTActivityListAsync(string NFtInfoId, List<int> types, long timestampMin,
        long timestampMax, int skipCount, int maxResultCount);
    
    public Task<NFTActivityIndex> GetCollectionActivityListAsync(string collectionId, List<string> bizIdList,
        List<int> types, int skipCount, int maxResultCount);
}

public class NFTActivityProvider : INFTActivityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;

    public NFTActivityProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }


    public async Task<NFTActivityIndex> GetNFTActivityListAsync(string NFtInfoId, List<int> types, long timestampMin,
        long timestampMax, int skipCount, int maxResultCount)
    {
        var graphQLResponse = await _graphQlHelper.QueryAsync<NFTActivityIndex>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$types:[Int!],$timestampMin:Long,$timestampMax:Long,$nFTInfoId:String) {
                    data:nftActivityList(input:{skipCount: $skipCount,maxResultCount:$maxResultCount,types:$types,timestampMin:$timestampMin,timestampMax:$timestampMax,nFTInfoId:$nFTInfoId}){
                        totalRecordCount,
                        indexerNftactivity:data{
                                            nftInfoId,
                                            type,
                                            from,
                                            to,
                                            amount,
                                            price,
                                            transactionHash,
                                            timestamp,
                                            priceTokenInfo{
                                              id,
                                              chainId,
                                              blockHash,
                                              blockHeight,
                                              previousBlockHash,
                                              symbol
                                            }
                         }
                    }
                }",
            Variables = new
            {
                skipCount = skipCount, maxResultCount = maxResultCount, types = types,
                timestampMin = timestampMin, timestampMax = timestampMax,
                nFTInfoId = NFtInfoId
            }
        });
        return graphQLResponse?.Data;
    }

    public async Task<NFTActivityIndex> GetCollectionActivityListAsync(string collectionId, List<string> bizIdList,
        List<int> types, int skipCount, int maxResultCount)
    {
        var graphQLResponse = await _graphQlHelper.QueryAsync<NFTActivityIndex>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$collectionId:String!,$types:[Int!],$bizIdList:[String]) {
                    data:collectionActivityList(input:{skipCount: $skipCount,maxResultCount:$maxResultCount,collectionId:$collectionId,types:$types,bizIdList:$bizIdList}){
                        totalRecordCount,
                        indexerNftactivity:data{
                                            nftInfoId,
                                            type,
                                            from,
                                            to,
                                            amount,
                                            price,
                                            transactionHash,
                                            timestamp,
                                            priceTokenInfo{
                                              id,
                                              chainId,
                                              blockHash,
                                              blockHeight,
                                              previousBlockHash,
                                              symbol
                                            }
                         }
                    }
                }",
            Variables = new
            {
                skipCount = skipCount, maxResultCount = maxResultCount, types = types,
                collectionId = collectionId, bizIdList = bizIdList
            }
        });
        return graphQLResponse?.Data;
    }
}