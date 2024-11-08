using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Nest;
using NFTMarketServer;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

public class NFTCollectionProvider : INFTCollectionProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly INESTRepository<UserBalanceIndex, string> _userBalanceIndexRepository;
    private readonly IObjectMapper _objectMapper;
    private const int QuerySize = 1000;
    private const int ChunkSize = 100;

    public NFTCollectionProvider(IGraphQLHelper graphQlHelper
    ,INFTCollectionExtensionProvider nftCollectionExtensionProvider
    ,IObjectMapper objectMapper,
    INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
    INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
    INESTRepository<UserBalanceIndex, string> userBalanceIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
        _objectMapper = objectMapper;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _userBalanceIndexRepository = userBalanceIndexRepository;
    }

    public async Task<IndexerNFTCollections> GetNFTCollectionsIndexAsync(long inputSkipCount,
        long inputMaxResultCount, List<string> addressList)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTCollections>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$addressList:[String!]!) {
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
                        #logoImage,
                        #featuredImageLink,
                        #description,
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
			    query($ids:[String!]!) {
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
                                            #logoImage,
                                            #featuredImageLink,
                                            #description,
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
			    query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!) {
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
			    query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!) {
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

    // public async Task<IndexerNFTCollectionExtension> GenerateNFTCollectionExtensionById(string chainId, string symbol)
    // {
    //     var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTCollectionExtension>>(new GraphQLRequest
    //     {
    //         Query = @"
			 //    query($chainId:String!,$symbol:String!) {
    //                 data:generateNFTCollectionExtensionById(dto:{chainId:$chainId,symbol:$symbol}){
    //                     itemTotal,
    //                     ownerTotal
    //                 }
    //             }",
    //         Variables = new
    //         {
    //             chainId,
    //             symbol
    //         }
    //     });
    //    
    //     var result = indexerCommonResult?.Data;
    //     if (result == null)
    //     {
    //         return new IndexerNFTCollectionExtension();
    //     }
    //     return result;
    // }
    
    public async Task<IndexerNFTCollectionExtension> GenerateNFTCollectionExtensionById(string chainId, string symbol)
    {
        if (CommonConstant.SeedCollectionSymbol.Equals(symbol))
        { 
            var collectionExtensionResultDto = 
                await CountSeedSymbolIndexAsync(chainId);
            return collectionExtensionResultDto;
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>
        {
            q => q.Term(i => i.Field(f => f.ChainId)
                .Value(chainId)),
            q => q.Term(i => i.Field(f => f.CollectionSymbol)
                .Value(symbol))
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
        //Exclude 1.Burned All NFT ( supply = 0 and issued = totalSupply) 2.Create Failed (supply=0 and issued=0)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source(
                        $"{CommonConstant.BurnedAllNftScript} || {CommonConstant.CreateFailedANftScript} || {CommonConstant.IssuedLessThenOneANftScript}")
                )
            )
        );
        QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        
        var itemTotal = 0;
        var nftIdsSet = new HashSet<string>();
        var dataList = new List<NFTInfoNewIndex>();
        do
        {
            var result = await _nftInfoNewIndexRepository.GetListAsync(Filter, skip: itemTotal, limit: QuerySize,
                sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
            dataList = result.Item2;
            if (dataList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var nftId in dataList.Select(i => i.Id))
            {
                nftIdsSet.Add(nftId);
            }
            itemTotal += dataList.Count;
        } while (!dataList.IsNullOrEmpty());
        var splitList = await SplitHashSetAsync(nftIdsSet, ChunkSize);
        //Prevent the number of nftids from being split into multiple batches again
        //and the data is saved in userSet.
        var userSet = new HashSet<string>();
        foreach (var nftIds in splitList)
        {
            await GenerateUserCountByNFTIdsAsync(nftIds, userSet);
        }

        return new IndexerNFTCollectionExtension()
        {
            ItemTotal = itemTotal,
            OwnerTotal = userSet.Count
        };
        
    }
    
    private async Task<IndexerNFTCollectionExtension>  CountSeedSymbolIndexAsync(string chainId)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>
        {
            q => q.Term(i => 
                i.Field(f => f.IsDeleteFlag).Value(false)),
            q => q.Term(i => 
                i.Field(f => f.ChainId).Value(chainId))
        };
        var mustNotQuery = new List<Func<QueryContainerDescriptor<SeedSymbolIndex>, QueryContainer>>();
        //Exclude 1.Burned All NFT ( supply = 0 and issued = totalSupply) 2.Create Failed (supply=0 and issued=0)
        mustNotQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{CommonConstant.BurnedAllNftScript} || {CommonConstant.CreateFailedANftScript}")
                )
            )
        );
        
        QueryContainer Filter(QueryContainerDescriptor<SeedSymbolIndex> f) =>
            f.Bool(b => b.Must(mustQuery).MustNot(mustNotQuery));
        var itemTotal = 0;
        var nftIdsSet = new HashSet<string>();
        List<SeedSymbolIndex> dataList;
        do
        {
            var result = await _seedSymbolIndexRepository.GetListAsync(Filter, skip: itemTotal, limit: QuerySize);
            dataList = result.Item2;
            if (dataList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var nftId in dataList.Select(i => i.Id))
            {
                nftIdsSet.Add(nftId);
            }
            itemTotal += dataList.Count;
        } while (!dataList.IsNullOrEmpty());
        var splitList = await SplitHashSetAsync(nftIdsSet, ChunkSize);
        //Prevent the number of nftids from being split into multiple batches again
        var userSet = new HashSet<string>();
        foreach (var nftIds in splitList)
        {
            await GenerateUserCountByNFTIdsAsync(nftIds, userSet);
        }

        return new IndexerNFTCollectionExtension()
        {
            ItemTotal = itemTotal,
            OwnerTotal = userSet.Count
        };

    }
    /**
     * Generate the number of accounts corresponding to nftIds
     */
    private async Task GenerateUserCountByNFTIdsAsync(
        HashSet<string> nftIds,
        HashSet<string> userSet)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i
            => i.Field(f => f.NFTInfoId).Terms(nftIds)));
        //query balance > 0 
        mustQuery.Add(q => q.Range(i
            => i.Field(f => f.Amount).GreaterThan(0)));

        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var skipCount = 0;
        var dataList = new List<UserBalanceIndex>();
        do
        {
            var result = await _userBalanceIndexRepository.GetListAsync(Filter, skip: skipCount, limit: QuerySize,
                sortType: SortOrder.Ascending, sortExp: o => o.BlockHeight);
            dataList = result.Item2;
            if (dataList.IsNullOrEmpty())
            {
                break;
            }
            foreach (var address in dataList.Select(i => i.Address))
            {
                userSet.Add(address);
            }
            skipCount += dataList.Count;
        } while (!dataList.IsNullOrEmpty());
    }
    
    private static async Task<List<HashSet<T>>> SplitHashSetAsync<T>(HashSet<T> source, int chunkSize)
    {
        var listOfHashSets = new List<HashSet<T>>();
        var currentHashSet = new HashSet<T>();
        foreach (var item in source)
        {
            currentHashSet.Add(item);
            if (currentHashSet.Count == chunkSize)
            {
                listOfHashSets.Add(currentHashSet);
                currentHashSet = new HashSet<T>();
            }
        }
        if (currentHashSet.Count > 0)
        {
            listOfHashSets.Add(currentHashSet);
        }
        return listOfHashSets;
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