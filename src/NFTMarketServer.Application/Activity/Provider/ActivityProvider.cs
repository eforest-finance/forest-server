using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Activity.Index;
using NFTMarketServer.Common;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Activity.Provider;

public class ActivityProvider : IActivityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IObjectMapper _objectMapper;
    
    public ActivityProvider(IGraphQLHelper graphQlHelper
        ,IObjectMapper objectMapper)
    {
        _graphQlHelper = graphQlHelper;
        _objectMapper = objectMapper;
    }
    
    public async Task<IndexerActivities> GetActivityListAsync(List<string> address, List<SymbolMarketActivityType> types, int skipCount,
        int maxResultCount)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerActivities>(new GraphQLRequest
        {
            Query = @"query(
                $skipCount: Int!
                ,$maxResultCount: Int!
                ,$address: [String!]!
                ,$types: [SymbolMarketActivityType!]!
            ){
                data: symbolMarketActivities(dto:{
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,address: $address
                ,types: $types}){
                        totalRecordCount,
                        indexerActivityList:data {
                           transactionDateTime,
                           symbol,
                           type,
                           price,
                           priceSymbol,
                           transactionFee,
                           transactionFeeSymbol,
                           transactionId
                        }
                }
            }",
            Variables = new
            {
                address = address,
                types = types,
                skipCount = skipCount,
                maxResultCount = maxResultCount
            }
        });
        return indexerCommonResult?.Data;
    }
}