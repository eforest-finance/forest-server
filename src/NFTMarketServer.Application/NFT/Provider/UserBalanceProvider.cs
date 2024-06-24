using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Users;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT.Provider;


public interface IUserBalanceProvider
{
    Task<IndexerNFTBalanceInfo> GetNFTBalanceInfoAsync(string nftInfoId);
    
    Task<IndexerUserMatchedNftIds> GetUserMatchedNftIdsAsync(GetNFTInfosProfileInput input, bool isSeed);
    
    public Task<UserBalanceIndexerListDto> QueryUserBalanceListAsync(QueryUserBalanceInput input);

}


public class UserBalanceProvider : IUserBalanceProvider, ISingletonDependency
{
    
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IObjectMapper _objectMapper;
    private readonly IGraphQLClientFactory _graphQlClientFactory;
    private const GraphQLClientEnum ClientType = GraphQLClientEnum.ForestClient;


    public UserBalanceProvider(IGraphQLHelper graphQlHelper,
        IObjectMapper objectMapper,
        IGraphQLClientFactory graphQlClientFactory)
    {
        _graphQlHelper = graphQlHelper;
        _objectMapper = objectMapper;
        _graphQlClientFactory = graphQlClientFactory;

    }
    
    public async Task<IndexerNFTBalanceInfo> GetNFTBalanceInfoAsync(string nftInfoId)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTBalanceInfo>>(new GraphQLRequest
        {
            Query = @"
			    query($nftInfoId:String!) {
                    data:queryUserBalanceByNftId(dto:{nftInfoId:$nftInfoId}){
                        owner,
                        ownerCount
                    }
                }",
            Variables = new
            {
                nftInfoId
            }
        });
        var result = indexerCommonResult?.Data ?? new IndexerNFTBalanceInfo();
        return result;
    }

    public async Task<IndexerUserMatchedNftIds> GetUserMatchedNftIdsAsync(GetNFTInfosProfileInput input, bool isSeed)
    {
        var indexerCommonResult  = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerUserMatchedNftIds>>(new GraphQLRequest
        {
            Query = @"query($skipCount: Int!
                    ,$maxResultCount: Int!
                    ,$nftCollectionId: String
                    ,$priceLow: Float
                    ,$priceHigh: Float
                    ,$status: Int!
                    ,$address: String
                    ,$issueAddress: String
                    ,$nftInfoIds: [String]
                    ,$isSeed: Boolean!
                ) {
                data: queryUserNftIdsPage(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,nftCollectionId: $nftCollectionId
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,status: $status
                ,address: $address
                ,issueAddress: $issueAddress
                ,nFTInfoIds: $nftInfoIds
                ,isSeed: $isSeed
                }) {
                        nftIds,
                        count
                    }
                }",
            Variables = new
            {
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                nftCollectionId = input.NFTCollectionId,
                priceLow = input.PriceLow,
                priceHigh = input.PriceHigh,
                address = input.Address,
                issueAddress = input.IssueAddress,
                status = input.Status,
                nftInfoIds = input.NFTInfoIds,
                isSeed
            }
        });
        var result = indexerCommonResult?.Data ?? new IndexerUserMatchedNftIds();
        return result;
    }

    public async Task<UserBalanceIndexerListDto> QueryUserBalanceListAsync(QueryUserBalanceInput input)
    {
        var client = _graphQlClientFactory.GetClient(ClientType);

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

        return indexerCommonResult?.Data.QueryUserBalanceList;
    }
}