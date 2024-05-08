using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

public class NFTCollectionProvider : INFTCollectionProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
    private readonly IObjectMapper _objectMapper;

    public NFTCollectionProvider(IGraphQLHelper graphQlHelper
    ,INFTCollectionExtensionProvider nftCollectionExtensionProvider
    ,IObjectMapper objectMapper)
    {
        _graphQlHelper = graphQlHelper;
        _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
        _objectMapper = objectMapper;
    }

    public async Task<IndexerNFTCollections> GetNFTCollectionsIndexAsync(long inputSkipCount,
        long inputMaxResultCount, List<string> addressList)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTCollections>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$addressList:[String]) {
                    data:nftCollectionsByAddressList(dto:{skipCount: $skipCount,maxResultCount:$maxResultCount,addressList:$addressList}){
                        totalRecordCount,
                        indexerNftCollections:data{
                                            id,
                                            chainId,
                                            symbol,
                                            tokenName,
                                            totalSupply,
                                            isBurnable,
                                            issueChainId,
                                            creatorAddress,
                                            proxyOwnerAddress,
                                            proxyIssuerAddress,
                                            logoImage,
                                            featuredImageLink,
                                            description,
                                            isOfficial,
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
                addressList = addressList
            }
        });
        return indexerCommonResult?.Data;
    }


    public async Task<IndexerNFTCollection> GetNFTCollectionIndexAsync(string inputId)
    {
        var indexerCommonResult =
            await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTCollection>>(new GraphQLRequest
            {
                Query = @"
			    query($id: String!) {
                    data:nftCollection(dto:{id: $id}) {
                        id,
                        chainId,
                        symbol,
                        tokenName,
                        totalSupply,
                        isBurnable,
                        issueChainId,
                        creatorAddress,
                        proxyOwnerAddress,
                        proxyIssuerAddress,
                        logoImage,
                        featuredImageLink,
                        description,
                        isOfficial,
                        externalInfoDictionary{
                          key,
                          value
                        },
                        createTime
                    }
                  }",
                Variables = new
                {
                    id = inputId
                }
            });

        var result = indexerCommonResult?.Data;
        if (result == null)
        {
            return null;
        }
        return result;
    }

    public async Task<Dictionary<string, IndexerNFTCollection>> GetNFTCollectionIndexByIdsAsync(List<string> inputIds)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTCollections>(new GraphQLRequest
        {
            Query = @"
			    query($ids:[String]!) {
                    data:nftCollectionByIds(dto:{ids: $ids}){
                        totalRecordCount,
                        indexerNftCollections:data{
                                            id,
                                            chainId,
                                            symbol,
                                            tokenName,
                                            totalSupply,
                                            isBurnable,
                                            issueChainId,
                                            creatorAddress,
                                            proxyOwnerAddress,
                                            proxyIssuerAddress,
                                            logoImage,
                                            featuredImageLink,
                                            description,
                                            isOfficial,
                                            externalInfoDictionary{
                                              key,
                                              value
                                            }
                                           }
                    }
                }",
            Variables = new
            {
                ids = inputIds
            }
        });
        var result = new Dictionary<string, IndexerNFTCollection>();
        var queryResult = indexerCommonResult?.Data;
        if (queryResult == null
            || queryResult.TotalRecordCount == null
            || queryResult.TotalRecordCount == 0
            || queryResult.IndexerNftCollections == null) 
        {
            return result;
        }
        var collectionExtensions = await _nftCollectionExtensionProvider.GetNFTCollectionExtensionsAsync(
            queryResult.IndexerNftCollections.Select(item => item.Id).ToList());

        foreach (IndexerNFTCollection tem in queryResult.IndexerNftCollections)
        {
            if (collectionExtensions.ContainsKey(tem.Id)
                && collectionExtensions[tem.Id] != null)
            {
                _objectMapper.Map(collectionExtensions[tem.Id], tem);
                tem.BaseUrl = collectionExtensions[tem.Id].ExternalLink;
            }

            result.Add(tem.Id, tem);
        }
        
        return result;
    }

    public async Task<IndexerNFTCollectionChanges> GetNFTCollectionChangesByBlockHeightAsync(int skipCount, string chainId, long startBlockHeight)
    {
        var indexerCommonResult =
            await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTCollectionChanges>>(new GraphQLRequest
            {
                Query = @"
			    query($skipCount:Int!,$chainId:String,$startBlockHeight:Long!) {
                    data:nftCollectionChange(dto:{skipCount:$skipCount,chainId:$chainId,blockHeight:$startBlockHeight}) {
                        totalRecordCount,
                        indexerNftCollectionChanges:data{
                             chainId,
                             symbol,
                             blockHeight
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
    
    public async Task<IndexerNFTCollectionPriceChanges> GetNFTCollectionPriceChangesByBlockHeightAsync(int skipCount, string chainId, long startBlockHeight)
    {
        var indexerCommonResult =
            await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTCollectionPriceChanges>>(new GraphQLRequest
            {
                Query = @"
			    query($skipCount:Int!,$chainId:String,$startBlockHeight:Long!) {
                    data:nftCollectionPriceChange(dto:{skipCount:$skipCount,chainId:$chainId,blockHeight:$startBlockHeight}) {
                        totalRecordCount,
                        indexerNftCollectionPriceChanges:data{
                             chainId,
                             symbol,
                             blockHeight
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

    public async Task<IndexerNFTCollectionExtension> GenerateNFTCollectionExtensionById(string chainId, string symbol)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTCollectionExtension>>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!,$symbol:String!) {
                    data:generateNFTCollectionExtensionById(dto:{chainId:$chainId,symbol:$symbol}){
                        itemTotal,
                        ownerTotal
                    }
                }",
            Variables = new
            {
                chainId,
                symbol
            }
        });
       
        var result = indexerCommonResult?.Data;
        if (result == null)
        {
            return new IndexerNFTCollectionExtension();
        }
        return result;
    }

    public async Task<IndexerNFTCollectionPrice> GetNFTCollectionPriceAsync(string chainId, string symbol, decimal floorPrice)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTCollectionPrice>>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!,$symbol:String!,$floorPrice:Decimal!) {
                    data:calcNFTCollectionPrice(dto:{chainId:$chainId,symbol:$symbol,floorPrice:$floorPrice}){
                        floorPrice
                    }
                }",
            Variables = new
            {
                chainId,
                symbol,
                floorPrice
            }
        });
       
        var result = indexerCommonResult?.Data;
        if (result == null)
        {
            return new IndexerNFTCollectionPrice();
        }
        return result;
    }
    
    public async Task<IndexerNFTCollectionTrade> GetNFTCollectionTradeAsync(string chainId, string collectionId,
        long beginUtcStamp, long endUtcStamp)
    {
        var collectionSymbol = IdGenerateHelper.GetCollectionIdSymbol(collectionId);
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTCollectionTrade>>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!,$collectionSymbol:String!,$collectionId:String!,$beginUtcStamp:Long!,$endUtcStamp:Long!) {
                    data:calcNFTCollectionTrade(dto:{chainId:$chainId,collectionSymbol:$collectionSymbol,collectionId:$collectionId,beginUtcStamp:$beginUtcStamp,endUtcStamp:$endUtcStamp}){
                        volumeTotal
                        salesTotal
                        floorPrice
                    }
                }",
            Variables = new
            {
                chainId,
                collectionSymbol,
                collectionId,
                beginUtcStamp,
                endUtcStamp
            }
        });
       
        var result = indexerCommonResult?.Data;
        if (result == null)
        {
            return new IndexerNFTCollectionTrade();
        }
        return result;
    }
}