using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT.Provider;

public class ListingPriceData : IndexerCommonResult<ListingPriceData>
{
    public PagedResultDto<IndexerListingWhitelistPrice> nftListingWhitelistPrices { get; set; }
}


public class NFTListingWhitelistPriceProvider : INFTListingWhitelistPriceProvider
{
    private readonly IGraphQLHelper _graphQlHelper;

    public NFTListingWhitelistPriceProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<List<IndexerListingWhitelistPrice>> GetNFTListingWhitelistPricesAsync(string inputAddress,
        List<string> inputNFTInfoIds)
    {
        if (inputNFTInfoIds.IsNullOrEmpty())
        {
            return new List<IndexerListingWhitelistPrice>();
        }

        var listingPriceList = await _graphQlHelper.QueryAsync<ListingPriceData>(new GraphQLRequest
        {
            Query = @"query (
                     $skipCount:Int!
                    ,$maxResultCount:Int!
                    ,$address:String!
                    ,$nftInfoIdList:[String]!
                ){
                    nftListingWhitelistPrices(dto:{
                        skipCount:$skipCount,
                        maxResultCount:$maxResultCount,
                        address:$address,
                        nftInfoIds:$nftInfoIdList
                    }){
                        TotalCount:totalRecordCount,
                        Message:message,
                        Items:data{
                            listingId,
                            quantity,
                            startTime,
                            publicTime,
                            expireTime,
                            durationHours,
                            offerFrom,
                            nftInfoId,
                            owner,
                            prices,
                            whiteListPrice,
                            whitelistId,
                            whitelistPriceToken:purchaseToken{
                                id,chainId,symbol,decimals
                            }
                
                        }
                    }
                }",
            Variables = new
            {
                skipCount = 0,
                maxResultCount = inputNFTInfoIds.Count,
                address = inputAddress,
                nftInfoIdList = inputNFTInfoIds
            }
        });
        return listingPriceList?.nftListingWhitelistPrices?.Items?.Select(i => i).ToList();

    }
}