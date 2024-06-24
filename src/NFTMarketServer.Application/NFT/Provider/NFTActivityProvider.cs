using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
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
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly INESTRepository<CollectionRelationIndex, string> _collectionRelationIndexRepository;
    private readonly INESTRepository<NFTActivityIndex, string> _nftActivityIndexRepository;

    public NFTActivityProvider(IGraphQLHelper graphQlHelper,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        INESTRepository<CollectionRelationIndex, string> collectionRelationIndexRepository,
        INESTRepository<NFTActivityIndex, string> nftActivityIndexRepository,
        ILogger<NFTActivityProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _collectionRelationIndexRepository = collectionRelationIndexRepository;
        _nftActivityIndexRepository = nftActivityIndexRepository;
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

    public async Task SaveOrUpdateNFTActivityInfoAsync(NFTActivitySyncDto activityDto)
    {

        if (activityDto == null)
        {
            return;
        }
        var symbolName = "";
        var collectionName = "";
        var collectionId = "";
        var decimals = 0;
        var image = "";
        var issuer = "";
        if (SymbolHelper.CheckSymbolIsCommonNFTInfoId(activityDto.NFTInfoId))
        {
            var nftInfoNewIndex = await _nftInfoNewIndexRepository.GetAsync(activityDto.NFTInfoId);
            if (nftInfoNewIndex == null)
            {
                return;
            }

            collectionId = nftInfoNewIndex.CollectionId;
            symbolName = nftInfoNewIndex.TokenName;
            collectionName = nftInfoNewIndex.CollectionName;
            decimals = nftInfoNewIndex.Decimals;
            issuer = nftInfoNewIndex.Issuer;
            image = SymbolHelper.BuildNFTImage(nftInfoNewIndex);
        }
        else
        {
            var seedInfoIndex = await _seedSymbolIndexRepository.GetAsync(activityDto.NFTInfoId);
            if (seedInfoIndex == null)
            {
                return;
            }

            collectionId = SymbolHelper.TransferNFTIdToCollectionId(activityDto.NFTInfoId);
            symbolName = seedInfoIndex.TokenName;
            collectionName = CommonConstant.CollectionSeedName;
            decimals = seedInfoIndex.Decimals;
            image = seedInfoIndex.SeedImage;
            issuer = seedInfoIndex.Issuer;
        }

        var from = FullAddressHelper.ToShortAddress(activityDto.From);
        var to = FullAddressHelper.ToShortAddress(activityDto.To);
        var fullFromAddress = FullAddressHelper.ToShortAddress(activityDto.From);
        var fullToAddress = FullAddressHelper.ToShortAddress(activityDto.To);

        var collectionRelationIndexList = BuildCollectionRelationIndexList(collectionId, from, to);
        await _collectionRelationIndexRepository.BulkAddOrUpdateAsync(collectionRelationIndexList);

        var nftActivityIndex = new NFTActivityIndex
        {
            Id = activityDto.Id,
            NftInfoId = activityDto.NFTInfoId,
            CollectionId = collectionId,
            CollectionName = collectionName,
            Decimals = decimals,
            NFTName = symbolName,
            From = from,
            FullFromAddress = fullFromAddress,
            To = to,
            FullToAddress = fullToAddress,
            Amount = activityDto.Amount,
            Price = activityDto.Price,
            TransactionHash = activityDto.TransactionHash,
            Timestamp = activityDto.Timestamp,
            NFTType = SymbolHelper.CheckSymbolIsCommonNFTInfoId(activityDto.NFTInfoId) ? NFTType.NFT : NFTType.Seed,
            NFTImage = image,
            ToNFTIssueFlag = to.Equals(issuer)
        };
        await _nftActivityIndexRepository.AddOrUpdateAsync(nftActivityIndex);

    }

    private static List<CollectionRelationIndex> BuildCollectionRelationIndexList(string collectionId,string from,string to)
    {
        if (from.IsNullOrEmpty() || to.IsNullOrEmpty())
        {
            return null;
        }
        var collectionRelationFrom = new CollectionRelationIndex()
        {
            Id = IdGenerateHelper.GetCollectionRelationId(collectionId,from),
            CollectionId = collectionId,
            Address = from
        };
        var collectionRelationList = new List<CollectionRelationIndex>()
        {
            collectionRelationFrom
        };
        if (from.Equals(to))
        {
            return collectionRelationList;
        }
        var collectionRelationTo = new CollectionRelationIndex()
        {
            Id = IdGenerateHelper.GetCollectionRelationId(collectionId,to),
            CollectionId = collectionId,
            Address = to
        };
        collectionRelationList.Add(collectionRelationTo);
        return collectionRelationList;
    }
}