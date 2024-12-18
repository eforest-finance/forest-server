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

    public Task<IndexerNFTActivityPage> GetMessageActivityListAsync(List<int> types, int skipCount, long startBlockHeight ,string chainId);
    
    public Task SaveOrUpdateNFTActivityInfoAsync(NFTActivitySyncDto nftActivitySyncDto);

    public Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedCollectionActivitiesAsync(
        GetCollectedCollectionActivitiesInput input, List<string> nftInfoIds);
    
    Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedActivityListAsync(GetCollectedActivityListDto dto);
    
    Task<Tuple<long, List<NFTActivityIndex>>> GetActivityByIdListAsync(List<string> idList);

    Task<Dictionary<string, string>> GetRecentNFTImageByCollectionIdList(List<string> collectionIds);

    Task<Tuple<long, List<NFTActivityIndex>>> GetActivityListAsync(List<string> addresses, List<int> types, long startTime,long endTime);

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
    private readonly IElasticClient _elasticClient;

    public NFTActivityProvider(IGraphQLHelper graphQlHelper,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        INESTRepository<CollectionRelationIndex, string> collectionRelationIndexRepository,
        INESTRepository<NFTActivityIndex, string> nftActivityIndexRepository,
        IElasticClient elasticClient,
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
        _elasticClient = elasticClient;
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

    //todo v2 change to local search
    // public async Task<IndexerNFTActivityPage> GetCollectionActivityListAsync(string collectionId, List<string> bizIdList,
    //     List<int> types, int skipCount, int maxResultCount)
    // {
    //     var graphQLResponse = await _graphQlHelper.QueryAsync<IndexerNFTActivityPage>(new GraphQLRequest
    //     {
    //         Query = @"
			 //    query($skipCount:Int!,$maxResultCount:Int!,$collectionId:String!,$types:[Int!]!,$bizIdList:[String!]!) {
    //                 data:collectionActivityList(input:{skipCount: $skipCount,maxResultCount:$maxResultCount,collectionId:$collectionId,types:$types,bizIdList:$bizIdList}){
    //                     totalRecordCount,
    //                     indexerNftactivity:data{
    //                                         nftInfoId,
    //                                         type,
    //                                         from,
    //                                         to,
    //                                         amount,
    //                                         price,
    //                                         transactionHash,
    //                                         timestamp,
    //                                         priceTokenInfo{
    //                                           id,
    //                                           chainId,
    //                                           blockHash,
    //                                           blockHeight,
    //                                           previousBlockHash,
    //                                           symbol
    //                                         }
    //                      }
    //                 }
    //             }",
    //         Variables = new
    //         {
    //             skipCount = skipCount, maxResultCount = maxResultCount, types = types,
    //             collectionId = collectionId, bizIdList = bizIdList
    //         }
    //     });
    //     return graphQLResponse?.Data;
    // }
    
    public async Task<IndexerNFTActivityPage> GetCollectionActivityListAsync(string collectionId, List<string> bizIdList,
        List<int> types, int skipCount, int maxResultCount)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();

        if (!bizIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.NftInfoId).Terms(bizIdList)));
        }
        if (types?.Count > 0)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Type).Terms(types)));
        }
        
        var collectionSymbolPre = NFTHelper.GetCollectionIdPre(collectionId);
        mustQuery.Add(q => q
            .Script(sc => sc
                .Script(script =>
                    script.Source($"{"doc['nftInfoId'].value.contains('"+collectionSymbolPre+"')"}")
                )
            )
        );

        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f) => f.Bool(b => b.Must(mustQuery));

        var list = await _nftActivityIndexRepository.GetSortListAsync(Filter, limit: maxResultCount,
            skip: skipCount, sortFunc: GetSortForNFTActivityIndexs());
        var dataList = _objectMapper.Map<List<NFTActivityIndex>, List<NFTActivityItem>>(list.Item2);

        var totalCount = list?.Item1;
        if (list?.Item1 == CommonConstant.EsLimitTotalNumber)
        {
            totalCount =
                await QueryRealCountAsync(_nftActivityIndexRepository, mustQuery, null);
        }
        
        return new IndexerNFTActivityPage
        {
            IndexerNftActivity = dataList,
            TotalRecordCount = (long)(totalCount == null ? 0 : totalCount),
        };
    }
    private static async Task<long> QueryRealCountAsync(INESTRepository<NFTActivityIndex, string> nftActivityIndexRepository,List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>> mustQuery,List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>> mustNotQuery)
    {
        var countRequest = new SearchRequest<NFTActivityIndex>
        {
            Query = new BoolQuery
            {
                Must = mustQuery != null && mustQuery.Any()
                    ? mustQuery
                        .Select(func => func(new QueryContainerDescriptor<NFTActivityIndex>()))
                        .ToList()
                        .AsEnumerable()
                    : Enumerable.Empty<QueryContainer>(),
                MustNot = mustNotQuery != null && mustNotQuery.Any()
                    ? mustNotQuery
                        .Select(func => func(new QueryContainerDescriptor<NFTActivityIndex>()))
                        .ToList()
                        .AsEnumerable()
                    : Enumerable.Empty<QueryContainer>()
            },
            Size = 0
        };
        
        Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer> queryFunc = q => countRequest.Query;
        var realCount = await nftActivityIndexRepository.CountAsync(queryFunc);
        return realCount.Count;
    }
    
    private static Func<SortDescriptor<NFTActivityIndex>, IPromise<IList<ISort>>> GetSortForNFTActivityIndexs()
    {
        SortDescriptor<NFTActivityIndex> sortDescriptor = new SortDescriptor<NFTActivityIndex>();
        sortDescriptor.Descending(a=>a.Timestamp);
        sortDescriptor.Ascending(a=>a.Type);
        IPromise<IList<ISort>> promise = sortDescriptor;
        return s => promise;
    } 

    public async Task<IndexerNFTActivityPage> GetMessageActivityListAsync(List<int> types, int skipCount,
        long startBlockHeight, string chainId)
    {
        var graphQLResponse = await _graphQlHelper.QueryAsync<IndexerNFTActivityPage>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$blockHeight:Long!,$types:[Int!],$chainId:String) {
                    data:messageActivityList(input:{skipCount: $skipCount,blockHeight:$blockHeight,types:$types,chainId:$chainId}){
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
                skipCount = skipCount,
                blockHeight = startBlockHeight,
                types = types,
                chainId = chainId
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
                _logger.LogInformation("SaveOrUpdateNFTActivityInfoAsync nft is null common nftId={A}",activityDto.NFTInfoId);
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
                _logger.LogInformation("SaveOrUpdateNFTActivityInfoAsync nft is null seed nftId={A}",activityDto.NFTInfoId);
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
            ToNFTIssueFlag = !issuer.IsNullOrEmpty() && !to.IsNullOrEmpty() && to.Equals(issuer),
            PriceTokenInfo = _objectMapper.Map<TokenInfoDto, TokenInfoIndex>(activityDto.PriceTokenInfo),
            ChainId = activityDto.ChainId,
            Type = activityDto.Type
        };
        await _nftActivityIndexRepository.AddOrUpdateAsync(nftActivityIndex);

        await BuildCollectionRelationIndexListAsync(collectionId, from, to);
    }

    private async Task BuildCollectionRelationIndexListAsync(string collectionId, string from, string to)
    {
        if (collectionId.IsNullOrEmpty())
        {
            return;
        }
        if (CollectionUtilities.IsNullOrEmpty(from) && CollectionUtilities.IsNullOrEmpty(to))
        {
            return;
        }
        
        if (!from.IsNullOrEmpty())
        {
            var collectionRelationFrom = new CollectionRelationIndex()
            {
                Id = IdGenerateHelper.GetCollectionRelationId(collectionId,from),
                CollectionId = collectionId,
                Address = from
            };
             await _collectionRelationIndexRepository.AddOrUpdateAsync(collectionRelationFrom);
        }
        
        
        if (to.IsNullOrEmpty())
        {
            return;
        }

        if (!from.IsNullOrEmpty() && from.Equals(to))
        {
            return;
        }
        
        var collectionRelationTo = new CollectionRelationIndex()
        {
            Id = IdGenerateHelper.GetCollectionRelationId(collectionId,to),
            CollectionId = collectionId,
            Address = to,
            
        };
        await _collectionRelationIndexRepository.AddOrUpdateAsync(collectionRelationTo);
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

    public async Task<Dictionary<string, string>> GetRecentNFTImageByCollectionIdList(List<string> collectionIds)
    {
        var resultDic = new Dictionary<string, string>();
        var tasks = new List<Task<string>>();

        foreach (var collectionId in collectionIds)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(collectionId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Type).Value(NFTActivityType.ListWithFixedPrice)));

            QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var task = _nftActivityIndexRepository.GetListAsync(Filter, sortType: SortOrder.Descending, sortExp: item => item.Timestamp);
            tasks.Add(task.ContinueWith(result =>
            {
                if (result == null || result.Result == null || result.Result.Item2.IsNullOrEmpty())
                {
                    return string.Empty;
                }

                return result.Result.Item2.First().NFTImage;
            }));
        }

        await Task.WhenAll(tasks);

        for (int i = 0; i < collectionIds.Count; i++)
        {
            resultDic[collectionIds[i]] = tasks[i].Result;
        }

        return resultDic;
    }
    
    public async Task<Tuple<long, List<NFTActivityIndex>>> GetActivityListAsync(List<string> addresses, List<int> types, long startTime, long endTime)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<NFTActivityIndex>, QueryContainer>>();
        if (addresses?.Count > 0)
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.From).Terms(addresses)));
        }
        
        if (types?.Count > 0)
        {
            mustQuery.Add(q => 
                q.Terms(i => i.Field(f => f.Type).Terms(types)));
        }

        if (startTime is >= 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.Timestamp)
                    .GreaterThanOrEquals(DateTime.UnixEpoch.AddMilliseconds((double)startTime))));
        }

        if (endTime is > 0)
        {
            mustQuery.Add(q => q.DateRange(i =>
                i.Field(f => f.Timestamp)
                    .LessThanOrEquals(DateTime.UnixEpoch.AddMilliseconds((double)endTime))));
        }

        QueryContainer Filter(QueryContainerDescriptor<NFTActivityIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _nftActivityIndexRepository.GetListAsync(Filter);

        return result;
    }
}