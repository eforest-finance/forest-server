using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public partial interface INFTActivityProvider
{
    public Task<IndexerNFTActivityPage> GetNFTActivityListAsync(string NFtInfoId, List<int> types, long timestampMin,
        long timestampMax, int skipCount, int maxResultCount);
    
    public Task<IndexerNFTActivityPage> GetCollectionActivityListAsync(string collectionId, List<string> bizIdList,
        List<int> types, int skipCount, int maxResultCount);

    public Task<IndexerNFTActivityPage> GetMessageActivityListAsync(List<int> types, int skipCount, long startBlockHeight);
    
    public Task SaveOrUpdateNFTActivityInfoAsync(NFTActivitySyncDto nftActivitySyncDto);
}

public class NFTActivityProvider : INFTActivityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly ILogger<NFTActivityProvider> _logger;

    public NFTActivityProvider(IGraphQLHelper graphQlHelper,
        ILogger<NFTActivityProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
    }


    public async Task<IndexerNFTActivityPage> GetNFTActivityListAsync(string NFtInfoId, List<int> types, long timestampMin,
        long timestampMax, int skipCount, int maxResultCount)
    {
        var graphQLResponse = await _graphQlHelper.QueryAsync<IndexerNFTActivityPage>(new GraphQLRequest
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

    public async Task<IndexerNFTActivityPage> GetCollectionActivityListAsync(string collectionId, List<string> bizIdList,
        List<int> types, int skipCount, int maxResultCount)
    {
        var graphQLResponse = await _graphQlHelper.QueryAsync<IndexerNFTActivityPage>(new GraphQLRequest
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

    public async Task<IndexerNFTActivityPage> GetMessageActivityListAsync(List<int> types, int skipCount, long startBlockHeight)
    {
        var graphQLResponse = await _graphQlHelper.QueryAsync<IndexerNFTActivityPage>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$blockHeight:Long!,$types:[Int!]) {
                    data:messageActivityList(input:{skipCount: $skipCount,blockHeight:$blockHeight,types:$types}){
                        totalRecordCount,
                        indexerNftactivity:data{
                                            id,
                                            nftInfoId,
                                            type,
                                            from,
                                            to,
                                            amount,
                                            price,
                                            transactionHash,
                                            timestamp,
                                            blockHeight,
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
                skipCount = skipCount, blockHeight = startBlockHeight, types = types
            }
        });
        return graphQLResponse?.Data;
    }

    public async Task SaveOrUpdateNFTActivityInfoAsync(NFTActivitySyncDto nftActivitySyncDto)
    {
        //todo
        throw new System.NotImplementedException();
    }
}