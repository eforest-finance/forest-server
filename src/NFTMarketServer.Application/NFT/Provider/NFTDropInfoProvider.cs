using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using GraphQL;
using NFTMarketServer.Common;

namespace NFTMarketServer.NFT.Provider;

public class NFTDropInfoProvider : INFTDropInfoProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IObjectMapper _objectMapper;
    public async Task<NFTDropInfoIndexList> GetNFTDropInfoIndexListAsync(GetNFTDropListInput input)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<NFTDropInfoIndexList>(new GraphQLRequest
        {
            Query = @"
			    query($type:Int!) {
                    data:nftDropList(dto:{type:$type}){
                        totalRecordCount,
                        dropInfoIndexList:data{
                            Id,
                            CollectionId,
                            StartTime,
                            ExpireTime,
                            ClaimMax,  
                            ClaimPrice,
                            MaxIndex,
                            TotalAmount,
                            ClaimAmount,
                            IsBurn,
                            State,
                            ClaimMax,
                        }
                    }
                }",
            Variables = new
            {
                type = input.Type
            }
        });
        
        return indexerCommonResult?.Data;
    }


    public async Task<NFTDropInfoIndex> GetNFTDropInfoIndexAsync(string dropId)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<NFTDropInfoIndex>(new GraphQLRequest
        {
            Query = @"
			    query($dropId:String!) {
                    data:nftDrop(dropId:$dropId){
                            Id,
                            CollectionId,
                            StartTime,
                            ExpireTime,
                            ClaimMax,  
                            ClaimPrice,
                            MaxIndex,
                            TotalAmount,
                            ClaimAmount,
                            IsBurn,
                            State,
                            ClaimMax,
                    }
                }",
            Variables = new
            {
                dropId = dropId
            }
        });

        return indexerCommonResult.Data;
    }
    
    
    public async Task<NFTDropClaimIndex> GetNFTDropClaimIndexAsync(string dropId, string address)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<NFTDropClaimIndex>(new GraphQLRequest
        {
            Query = @"
			    query($dropId:String!, $address:String!) {
                    data:dropClaim(dto:{dropId:$dropId, address:$address}){
                            DropId,
                            ClaimLimit,
                            ClaimAmount,
                    }
                }",
            Variables = new
            {
                dropId = dropId,
                address = address
            }
        });

        return indexerCommonResult.Data;
    }

    public async Task<NFTDropInfoIndexList> GetExpireNFTDropListAsync()
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<NFTDropInfoIndexList>(new GraphQLRequest
        {
            Query = @"
			    query() {
                    data:expiredDropList{
                        totalRecordCount,
                        dropInfoIndexList:data{
                            Id,
                            CollectionId,
                            StartTime,
                            ExpireTime,
                            ClaimMax,  
                            ClaimPrice,
                            MaxIndex,
                            TotalAmount,
                            ClaimAmount,
                            IsBurn,
                            State,
                            ClaimMax,
                        }
                    }
                }"
        });
        
        return indexerCommonResult?.Data;
    }
}