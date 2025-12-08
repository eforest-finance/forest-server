using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface ISeedInfoProvider
{
    public Task<IndexerSeedBriefInfos> GetSeedBriefInfosAsync(GetCompositeNFTInfosInput input);

    public Task<IndexerSeedMainChainChangePage> GetIndexerSeedMainChainChangePageByBlockHeightAsync(int skipCount,
        string chainId, long startBlockHeight);
    
    public Task<IndexerSeedInfos> GetSeedInfosUserProfileAsync(GetNFTInfosProfileInput input);
}

public class SeedInfoProvider : ISeedInfoProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;

    public SeedInfoProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<IndexerSeedBriefInfos> GetSeedBriefInfosAsync(GetCompositeNFTInfosInput input)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerSeedBriefInfos>(new GraphQLRequest
        {
            Query = @"query($skipCount: Int!
                    ,$maxResultCount: Int!
                    ,$sorting: String!
                    ,$priceLow: Decimal
                    ,$priceHigh: Decimal
                    ,$hasListingFlag: Boolean!
                    ,$hasAuctionFlag: Boolean!
                    ,$hasOfferFlag: Boolean!
                    ,$searchParam: String
                    ,$chainList: [String!]
                    ,$symbolTypeList: [TokenType!]
                ) {
                data: seedBriefInfos(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,sorting: $sorting
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,hasListingFlag:$hasListingFlag
                ,hasAuctionFlag:$hasAuctionFlag
                ,hasOfferFlag:$hasOfferFlag
                ,searchParam:$searchParam
                ,chainList:$chainList
                ,symbolTypeList:$symbolTypeList
                }) {
                        totalRecordCount
                        indexerSeedBriefInfoList:data {
                          collectionSymbol
                          nFTSymbol
                          previewImage 
                          priceDescription 
                          price
                          id
                          tokenName
                          issueChainIdStr
                          chainIdStr
                        }
                    }
                }",
            Variables = new
            {
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                sorting = input.Sorting,
                priceLow = input.PriceLow,
                priceHigh = input.PriceHigh,
                hasListingFlag = input.HasListingFlag,
                hasAuctionFlag = input.HasAuctionFlag,
                hasOfferFlag = input.HasOfferFlag,
                searchParam = input.SearchParam,
                chainList = input.ChainList,
                symbolTypeList = input.SymbolTypeList
            }
        });
        return indexerCommonResult?.Data;
    }

    public async Task<IndexerSeedMainChainChangePage> GetIndexerSeedMainChainChangePageByBlockHeightAsync(int skipCount,
        string chainId, long startBlockHeight)
    {
        var indexerCommonResult =
            await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerSeedMainChainChangePage>>(new GraphQLRequest
            {
                Query = @"
			    query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!) {
                    data:seedMainChainChange(dto:{skipCount:$skipCount,chainId:$chainId,blockHeight:$startBlockHeight}) {
                        totalRecordCount,
                        indexerSeedMainChainChangeList:data{
                             chainId,
                             symbol,
                             blockHeight
                            transactionId
                        }
                    }
                  }",
                Variables = new
                {
                    skipCount,
                    chainId,
                    startBlockHeight
                }
            });
        return indexerCommonResult?.Data;
    }

    public async Task<IndexerSeedInfos> GetSeedInfosUserProfileAsync(GetNFTInfosProfileInput input)
    {
        var indexerCommonResult  = await _graphQlHelper.QueryAsync<IndexerSeedInfos>(new GraphQLRequest
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
                ) {
                data: seedInfosForUserProfile(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,nftCollectionId: $nftCollectionId
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,status: $status
                ,address: $address
                ,issueAddress: $issueAddress
                ,nFTInfoIds: $nftInfoIds
                }) {
                        totalRecordCount
                        indexerSeedInfoList:data {
                          id,
                          chainId,
                          symbol,
                          tokenContractAddress,
                          decimals,
                          supply,
                          totalSupply,
                          tokenName,
                          issuer,
                          isBurnable,
                          issueChainId,
                          issued,
                          createTime,
                          seedOwnedSymbol,
                          seedExpTimeSecond,
                          seedExpTime,
                          listingId,
                          listingAddress,
                          listingPrice,
                          listingQuantity,
                          listingEndTime,
                          latestListingTime,
                          seedImage,
                          owner,
                          otherOwnerListingFlag,
                          seedType,
                          tokenType,
                          registerTimeSecond,
                          hasOfferFlag,
                          hasListingFlag,
                          minListingPrice,
                          maxAuctionPrice,
                          maxOfferPrice,
                          externalInfoDictionary {
                            key,
                            value
                          },
                          listingToken {
                            id,
                            chainId,
                            symbol,
                            address:issuer,
                            decimals
                          },
                          latestDealToken {
                            id,
                            chainId,
                            symbol,
                            address:issuer,
                            decimals
                          }
                        }
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
                nftInfoIds = input.NFTInfoIds
            }
        });
        return indexerCommonResult?.Data;
    }
}