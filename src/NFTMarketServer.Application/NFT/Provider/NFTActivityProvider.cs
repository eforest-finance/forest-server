using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Nest;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Helper;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Dto;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Etos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using TokenInfoDto = NFTMarketServer.NFT.Dtos.TokenInfoDto;

namespace NFTMarketServer.NFT.Provider;

public partial interface INFTActivityProvider
{
    public Task<IndexerNFTActivityPage> GetNFTActivityListAsync(string NFtInfoId, List<int> types, long timestampMin,
        long timestampMax, int skipCount, int maxResultCount);
    
    public Task<IndexerNFTActivityPage> GetCollectionActivityListAsync(string collectionId, List<string> bizIdList,
        List<int> types, int skipCount, int maxResultCount);

    public Task<IndexerNFTActivityPage> GetMessageActivityListAsync(List<int> types, int skipCount, long startBlockHeight);
    
    public Task SaveOrUpdateNFTActivityInfoAsync(NFTActivitySyncDto nftActivitySyncDto);

    public Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedCollectionActivitiesAsync(
        GetCollectedCollectionActivitiesInput input, List<string> nftInfoIds);
    
    Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedActivityListAsync(GetCollectedActivityListDto dto);
    
    Task<Tuple<long, List<NFTActivityIndex>>> GetActivityByIdListAsync(List<string> idList);
}

public class NFTActivityProvider : INFTActivityProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly ILogger<NFTActivityProvider> _logger;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly INESTRepository<CollectionRelationIndex, string> _collectionRelationIndexRepository;
    private readonly INESTRepository<NFTActivityIndex, string> _nftActivityIndexRepository;
    private readonly IObjectMapper _objectMapper;

    public NFTActivityProvider(IGraphQLHelper graphQlHelper,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        INESTRepository<CollectionRelationIndex, string> collectionRelationIndexRepository,
        INESTRepository<NFTActivityIndex, string> nftActivityIndexRepository,
        ILogger<NFTActivityProvider> logger,
        IObjectMapper objectMapper)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _collectionRelationIndexRepository = collectionRelationIndexRepository;
        _nftActivityIndexRepository = nftActivityIndexRepository;
        _objectMapper = objectMapper;
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
                                            chainId,
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
                                              symbol,
                                              decimals
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
        var fullFromAddress = FullAddressHelper.ToFullAddress(activityDto.From, activityDto.ChainId);
        var fullToAddress = FullAddressHelper.ToFullAddress(activityDto.To, activityDto.ChainId);

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
            ToNFTIssueFlag = to.Equals(issuer),
            PriceTokenInfo = _objectMapper.Map<TokenInfoDto, TokenInfoIndex>(activityDto.PriceTokenInfo),
            ChainId = activityDto.ChainId,
            Type = activityDto.Type
        };
        await _nftActivityIndexRepository.AddOrUpdateAsync(nftActivityIndex);

    }

    private static List<CollectionRelationIndex> BuildCollectionRelationIndexList(string collectionId,string from,string to)
    {
        if (CollectionUtilities.IsNullOrEmpty(from) || CollectionUtilities.IsNullOrEmpty(to))
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
            Address = to,
            
        };
        collectionRelationList.Add(collectionRelationTo);
        return collectionRelationList;
    }

    public async Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedCollectionActivitiesAsync(
        GetCollectedCollectionActivitiesInput input, List<string> nftInfoIds)
    {
        if (input == null || input.Address.IsNullOrEmpty())
        {
            return null;
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();

        if (!input.CollectionIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.CollectionId).Terms(input.CollectionIdList)));
        }
        
        if (!input.Type.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.Type).Terms(input.Type)));
        }

        if (!input.ChainList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.ChainId).Terms(input.ChainList)));
        }
        if (!nftInfoIds.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.NftInfoId).Terms(nftInfoIds)));
        }
        
        var shouldQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();

        shouldQuery.Add(q => q.Terms(i => i.Field(f => f.From).Terms(input.Address)));
        shouldQuery.Add(q => q.Term(i => i.Field(f => f.To).Value(input.Address)));

        if (shouldQuery.Any())
        {
            mustQuery.Add(q => q.Bool(b => b.Should(shouldQuery)));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftActivityIndexRepository.GetListAsync(Filter, sortType: SortOrder.Descending,
            sortExp: item => item.Timestamp, skip: input.SkipCount, limit: input.MaxResultCount);
        
        return result;
    }

    public async Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedActivityListAsync(GetCollectedActivityListDto dto)
    {
        if (dto == null)
        {
            return null;
        }

        if (dto.FromAddress.IsNullOrEmpty() && dto.ToAddress.IsNullOrEmpty())
        {
            return null;
        }
        
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();

        if (!dto.CollectionIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.CollectionId).Terms(dto.CollectionIdList)));
        }

        if (!dto.ChainList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.ChainId).Terms(dto.ChainList)));
        }

        if (!dto.NFTInfoIds.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.NftInfoId).Terms(dto.NFTInfoIds)));
        }

        if (!dto.TypeList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(dto.TypeList)));
        }

        if (!dto.FromAddress.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.From).Terms(dto.FromAddress)));
        }

        if (!dto.ToAddress.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.To).Terms(dto.ToAddress)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftActivityIndexRepository.GetListAsync(Filter, sortType: SortOrder.Descending,
            sortExp: item => item.Timestamp, skip: dto.SkipCount, limit: dto.MaxResultCount);

        return result;
    }

    public async Task<Tuple<long, List<NFTActivityIndex>>> GetActivityByIdListAsync(List<string> idList)
    {
        if (idList.IsNullOrEmpty())
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();
        mustQuery.Add(q =>
            q.Terms(i => i.Field(f => f.Id).Terms(idList)));
        
        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftActivityIndexRepository.GetListAsync(Filter, sortType: SortOrder.Descending,
            sortExp: item => item.Timestamp);

        return result;
    }
}