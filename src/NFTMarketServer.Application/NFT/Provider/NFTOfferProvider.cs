using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface INFTOfferProvider
{
    public Task<IndexerNFTOffers> GetNFTOfferIndexesAsync(int skipCount, int maxResultCount,
        string chainId, List<string> chainIdList, string nftInfoId, string offerFrom, string offerTo);
    
    public Task<IndexerNFTOffer> GetMaxOfferInfoAsync(string nftInfoId);
    Task<List<ExpiredNftMaxOfferDto>> GetNftMaxOfferAsync(string chainId, long expiredSecond);
    public Task<List<NFTOfferChangeDto>> GetNFTOfferChangeAsync(string chainId, long blockHeight);
}

public class NFTOfferProvider : INFTOfferProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;

    public NFTOfferProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<IndexerNFTOffers> GetNFTOfferIndexesAsync(int inputSkipCount,
        int inputMaxResultCount,
        string inputChainId,
        List<string> inputChainIdList,
        string inputNFTInfoId, string inputOfferFrom, string inputOfferTo)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTOffers>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$chainId:String,$chainIdList:[String],$nftInfoId:String,$expireTimeGt:Long,$offerFrom:String,$offerTo:String) {
                    data:nftOffers(dto:{skipCount:$skipCount,maxResultCount:$maxResultCount,chainId:$chainId,chainIdList:$chainIdList,nFTInfoId:$nftInfoId,expireTimeGt:$expireTimeGt,offerFrom:$offerFrom,offerTo:$offerTo}){
                        totalRecordCount,
                        indexerNFTOfferList:data{
                          bizSymbol,
                          bizInfoId,
                          id,
                          chainId,
                          from,
                          to,
                          price,
                          quantity,
                          realQuantity,
                          expireTime,
                          purchaseToken{
                            id,chainId,symbol,decimals,address:issuer
                          },
                          
                       }
                    }
                }",
            Variables = new
            {
                skipCount = inputSkipCount,
                maxResultCount = inputMaxResultCount,
                chainId = inputChainId,
                chainIdList = inputChainIdList,
                nftInfoId = inputNFTInfoId,
                expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                offerFrom = inputOfferFrom,
                offerTo = inputOfferTo
            }
        });
        return indexerCommonResult?.Data;
    }

    public async Task<IndexerNFTOffer> GetMaxOfferInfoAsync(string nftInfoId)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTOffer>(new GraphQLRequest
        {
            Query = @"
			    query($nftInfoId:String!) {
                    data:getMaxOfferInfo(dto:{nftInfoId:$nftInfoId}){
                          id,
                          chainId,
                          from,
                          to,
                          price,
                          quantity,
                          expireTime,
                          purchaseToken{
                            id,chainId,symbol,decimals,address:issuer
                          },
                          bizInfoId,
                          bizSymbol,
                          realQuantity   
                    }
                }",
            Variables = new
            {
                nftInfoId
            }
        });
        return indexerCommonResult?.Data;
    }
    
    public async Task<List<ExpiredNftMaxOfferDto>> GetNftMaxOfferAsync(string chainId, long expiredSecond)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<ExpiredNftMaxOfferResultDto>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!, $expireTimeGt:Long!) {
                    getExpiredNftMaxOffer(input:
                    {
                      chainId:$chainId,
                      expireTimeGt:$expireTimeGt
                    }){
                      key,
                      value {
                        id,
                        expireTime,
                        prices,
                        symbol
                       }
                    }
                }",
            Variables = new
            {
                chainId = chainId, 
                expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddSeconds(-expiredSecond))
            }
        });
        if (graphQlResponse == default(ExpiredNftMaxOfferResultDto))
        {
            return new List<ExpiredNftMaxOfferDto>();
        }

        return graphQlResponse.GetExpiredNftMaxOffer.IsNullOrEmpty()
            ? new List<ExpiredNftMaxOfferDto>()
            : graphQlResponse.GetExpiredNftMaxOffer;
    }
    
    public async Task<List<NFTOfferChangeDto>> GetNFTOfferChangeAsync(string chainId, long blockHeight)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<NFTOfferChangeResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!, $blockHeight:Long!){
            getNftOfferChange(dto: 
            {
                chainId:$chainId,
                blockHeight:$blockHeight
            })
            {
                nftId,
                chainId,
                blockHeight
            }}",
            Variables = new
            {
                chainId,
                blockHeight
            }
        });
        return graphQlResponse.GetNFTOfferChange.IsNullOrEmpty() ? new List<NFTOfferChangeDto>() : graphQlResponse.GetNFTOfferChange;
    }
}