using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public class NFTInfoProvider : INFTInfoProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;

    public NFTInfoProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }
    
    public async Task<IndexerNFTInfos> GetNFTInfoIndexsAsync(int inputSkipCount,
        int inputMaxResultCount,
        string inputNFTCollectionId,
        string inputSorting,
        decimal inputPriceLow,
        decimal inputPriceHigh,
        int inputStatus,
        string inputAddress,
        string inputIssueAddress,
        List<string> inputNFTInfoIds)
    {
        var indexerCommonResult =  await _graphQlHelper.QueryAsync<IndexerNFTInfos>(new GraphQLRequest
        {
            Query = @"query($skipCount: Int!
                    ,$maxResultCount: Int!
                    ,$nftCollectionId: String
                    ,$sorting: String!
                    ,$priceLow: Float
                    ,$priceHigh: Float
                    ,$status: Int!
                    ,$address: String
                    ,$issueAddress: String
                    ,$nftInfoIds: [String]
                ) {
                data: nftInfos(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,nftCollectionId: $nftCollectionId
                ,sorting: $sorting
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,status: $status
                ,address: $address
                ,issueAddress: $issueAddress
                ,nFTInfoIds: $nftInfoIds
                }) {
                        totalRecordCount
                        indexerNftInfos:data {
                          id,
                          collectionId,
                          chainId,
                          issueChainId,
                          symbol,
                          issuer,
                          proxyIssuerAddress,
                          imageUrl,
                          tokenName,
                          totalSupply,
                          otherOwnerListingFlag,
                          listingId,
                          listingAddress,
                          listingPrice,
                          listingQuantity,
                          listingEndTime,
                          latestListingTime,
                          latestDealPrice,
                          latestDealTime,
                          previewImage,
                          file,
                          fileExtension,
                          description,
                          isOfficial,
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
                          },
                            externalInfoDictionary{
                                              key,
                                              value
                                            }
                        }
                    }
                }",
                Variables = new
                {
                    skipCount = inputSkipCount,
                    maxResultCount = inputMaxResultCount,
                    nftCollectionId = inputNFTCollectionId,
                    sorting = inputSorting,
                    priceLow = inputPriceLow,
                    priceHigh = inputPriceHigh,
                    address = inputAddress,
                    issueAddress = inputIssueAddress,
                    status = inputStatus,
                    nftInfoIds = inputNFTInfoIds
                }
        });
        return indexerCommonResult?.Data;
    }

    public async Task<IndexerNFTInfos> GetNFTInfoIndexsUserProfileAsync(GetNFTInfosProfileInput input)
    {
        var indexerCommonResult  = await _graphQlHelper.QueryAsync<IndexerNFTInfos>(new GraphQLRequest
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
                data: nftInfosForUserProfile(dto: {
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
                        indexerNftInfos:data {
                          id,
                          hasListingFlag,
                          minListingPrice,
                          collectionId,
                          chainId,
                          issueChainId,
                          symbol,
                          issuer,
                          proxyIssuerAddress,
                          imageUrl,
                          tokenName,
                          totalSupply,
                          otherOwnerListingFlag,
                          listingId,
                          listingAddress,
                          listingPrice,
                          listingQuantity,
                          listingEndTime,
                          latestListingTime,
                          latestDealPrice,
                          latestDealTime,
                          previewImage,
                          file,
                          fileExtension,
                          description,
                          isOfficial,
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
                          },
                            externalInfoDictionary{
                                              key,
                                              value
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

    private async Task<IndexerNFTInfos> QueryForCommonNFTInfosAsync(GetNFTInfosInput input)
    {
        return await _graphQlHelper.QueryAsync<IndexerNFTInfos>(new GraphQLRequest
        {
            Query = @"query($skipCount: Int!
                    ,$maxResultCount: Int!
                    ,$nftCollectionId: String
                    ,$sorting: String!
                    ,$priceLow: Float
                    ,$priceHigh: Float
                    ,$status: Int!
                    ,$address: String
                    ,$issueAddress: String
                    ,$nftInfoIds: [String]
                ) {
                data: nftInfos(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,nftCollectionId: $nftCollectionId
                ,sorting: $sorting
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,status: $status
                ,address: $address
                ,issueAddress: $issueAddress
                ,nFTInfoIds: $nftInfoIds
                }) {
                        totalRecordCount
                        indexerNftInfos:data {
                          id,
                          collectionId,
                          chainId,
                          issueChainId,
                          symbol,
                          issuer,
                          proxyIssuerAddress,
                          imageUrl,
                          tokenName,
                          totalSupply,
                          otherOwnerListingFlag,
                          listingId,
                          listingAddress,
                          listingPrice,
                          listingQuantity,
                          listingEndTime,
                          latestListingTime,
                          latestDealPrice,
                          latestDealTime,
                          previewImage,
                          file,
                          fileExtension,
                          description,
                          isOfficial,
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
                          },
                            externalInfoDictionary{
                                              key,
                                              value
                                            }
                        }
                    }
                }",
            Variables = new
            {
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                nftCollectionId = input.NFTCollectionId,
                sorting = input.Sorting,
                priceLow = input.PriceLow,
                priceHigh = input.PriceHigh,
                address = input.Address,
                issueAddress = input.IssueAddress,
                status = input.Status,
                nftInfoIds = input.NFTInfoIds
            }
        });
    }

    private async Task<IndexerNFTInfos> QueryForSeedNFTInfosAsync(GetNFTInfosInput input)
    {
        return await _graphQlHelper.QueryAsync<IndexerNFTInfos>(new GraphQLRequest
        {
            Query = @"query($skipCount: Int!
                    ,$maxResultCount: Int!
                    ,$nftCollectionId: String
                    ,$sorting: String!
                    ,$priceLow: Float
                    ,$priceHigh: Float
                    ,$status: Int!
                    ,$address: String
                    ,$issueAddress: String
                    ,$nftInfoIds: [String]
                ) {
                data: nftInfos(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,nftCollectionId: $nftCollectionId
                ,sorting: $sorting
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,status: $status
                ,address: $address
                ,issueAddress: $issueAddress
                ,nFTInfoIds: $nftInfoIds
                }) {
                        totalRecordCount
                        indexerNftInfos:data {
                          id,
                          collectionId,
                          chainId,
                          issueChainId,
                          symbol,
                          issuer,
                          proxyIssuerAddress,
                          imageUrl,
                          tokenName,
                          totalSupply,
                          otherOwnerListingFlag,
                          listingId,
                          listingAddress,
                          listingPrice,
                          listingQuantity,
                          listingEndTime,
                          latestListingTime,
                          latestDealPrice,
                          latestDealTime,
                          previewImage,
                          file,
                          fileExtension,
                          description,
                          isOfficial,
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
                          },
                            externalInfoDictionary{
                                              key,
                                              value
                                            }
                        }
                    }
                }",
            Variables = new
            {
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                nftCollectionId = input.NFTCollectionId,
                sorting = input.Sorting,
                priceLow = input.PriceLow,
                priceHigh = input.PriceHigh,
                address = input.Address,
                issueAddress = input.IssueAddress,
                status = input.Status,
                nftInfoIds = input.NFTInfoIds
            }
        });
    }

    public async Task<IndexerNFTInfo> GetNFTInfoIndexAsync(string inputId, string inputAddress)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTInfo>(new GraphQLRequest
        {
            Query = @"
			    query(
                    $id:String!
                    ,$address:String
                ) {
                    data:nftInfo(dto:{id:$id,address:$address}){
                        id,
                        collectionId,
                        whitelistId,
                        chainId,
                        issueChainId,
                        symbol,
                        issuer,
                        proxyIssuerAddress,
                        owner,
                        ownerCount,
                        imageUrl,
                        tokenName,
                        totalSupply,
                        otherOwnerListingFlag,
                        listingId,
                        listingAddress,
                        listingPrice,
                        listingQuantity,
                        listingEndTime,
                        latestListingTime,
                        latestDealPrice,
                        latestDealTime,
                        previewImage,
                        file,
                        fileExtension,
                        description,
                        isOfficial,
                        listingToken {id, chainId, symbol, address:issuer, decimals},
                        externalInfoDictionary{key, value},
                        seedOwnedSymbol,
                        seedType,
                        tokenType,
                        registerTimeSecond,
                        seedExpTimeSecond
                    }
                }",
            Variables = new
            {
                id = inputId, address = inputAddress
            }
        });
        return indexerCommonResult?.Data;
    }

    public async Task<IndexerSymbol> GetNFTCollectionSymbolAsync(string inputSymbol)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerSymbol>(new GraphQLRequest
        {
            Query = @"
			    query($symbol:String!) {
                  data:nftCollectionSymbol(symbol:$symbol){symbol}
                }",
            Variables = new
            {
                symbol = inputSymbol
            }
        });
        return result?.Data;
    }

    public async Task<IndexerSymbol> GetNFTSymbolAsync(string inputSymbol)
    {
        var result = await _graphQlHelper.QueryAsync<IndexerSymbol>(new GraphQLRequest
        {
            Query = @"
			    query($symbol:String!) {
                  data:nftSymbol(symbol:$symbol){symbol}
                }",
            Variables = new
            {
                symbol = inputSymbol
            }
        });
        return result?.Data;
    }

    public async Task<IndexerNFTInfo> GetNFTSupplyAsync(string nftInfoId)
    {
        return await _graphQlHelper.QueryAsync<IndexerNFTInfo>(new GraphQLRequest
        {
            Query = @"
			    query nftInfo($id:Long!
                ) {
                    nftInfoIndex(){
                        totalCount,
                        data{
                            Id,
                            ChainId,
                            Symbol,
                            Issuer,
                            TokenName,
                            Issued,
                            TotalSupply,
                        }
                    }
                }",
            Variables = new
            {
                id = nftInfoId
            }
        });
    }

    public async Task<IndexerNFTBriefInfos> GetNFTBriefInfosAsync(GetCompositeNFTInfosInput input)
    {
        var indexerCommonResult =  await _graphQlHelper.QueryAsync<IndexerNFTBriefInfos>(new GraphQLRequest
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
                    ,$collectionId: String!
                    ,$chainList: [String!]
                    ,$symbolTypeList: [TokenType!]
                ) {
                data: nftBriefInfos(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,sorting: $sorting
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,hasListingFlag:$hasListingFlag
                ,hasAuctionFlag:$hasAuctionFlag
                ,hasOfferFlag:$hasOfferFlag
                ,searchParam:$searchParam
                ,collectionId: $collectionId
                ,chainList:$chainList
                ,symbolTypeList:$symbolTypeList
                }) {
                        totalRecordCount
                        indexerNFTBriefInfoList:data {
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
                collectionId = input.CollectionId,
                chainList = input.ChainList,
                symbolTypeList = input.SymbolTypeList
            }
        });
        return indexerCommonResult?.Data;
    }

    public async Task<IndexerNFTOwners> GetNFTOwnersAsync(GetNFTOwnersInput input)
    {
        var indexerCommonResult =  await _graphQlHelper.QueryAsync<IndexerNFTOwners>(new GraphQLRequest
        {
            Query = @"query($skipCount: Int!
                    ,$maxResultCount: Int!
                    ,$nftInfoId: String!
                    ,$chainId: String!
                ) {
                data: queryOwnersByNftId(input: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,nftInfoId: $nftInfoId
                ,chainId: $chainId
                }) {
                        totalCount
                        indexerNftUserBalances:data {
                          id,
                          address,
                          amount
                        }
                    }
                }",
            Variables = new
            {
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                nftInfoId = input.Id,
                chainId = input.ChainId
            }
        });

        return indexerCommonResult?.Data;
    }
}