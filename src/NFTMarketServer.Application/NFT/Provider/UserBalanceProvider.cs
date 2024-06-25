using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using Nest;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Dtos;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Users;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT.Provider;


public interface IUserBalanceProvider
{
    Task<IndexerNFTBalanceInfo> GetNFTBalanceInfoAsync(string nftInfoId);
    
    Task<IndexerUserMatchedNftIds> GetUserMatchedNftIdsAsync(GetNFTInfosProfileInput input, bool isSeed);
    
    public Task<UserBalanceIndexerListDto> QueryUserBalanceListAsync(QueryUserBalanceInput input);

    public Task<List<string>> GetNFTIdListByUserBalancesAsync(GetCollectedCollectionActivitiesInput input,
        int skipCount, int maxResultCount);
}


public class UserBalanceProvider : IUserBalanceProvider, ISingletonDependency
{
    
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IObjectMapper _objectMapper;
    private readonly IGraphQLClientFactory _graphQlClientFactory;
    private const GraphQLClientEnum ClientType = GraphQLClientEnum.ForestClient;
    private readonly INESTRepository<UserBalanceIndex, string> _userBalanceIndexRepository;


    public UserBalanceProvider(IGraphQLHelper graphQlHelper,
        IObjectMapper objectMapper,
        IGraphQLClientFactory graphQlClientFactory,
        INESTRepository<UserBalanceIndex, string> userBalanceIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _objectMapper = objectMapper;
        _graphQlClientFactory = graphQlClientFactory;
        _userBalanceIndexRepository = userBalanceIndexRepository;

    }
    
    public async Task<IndexerNFTBalanceInfo> GetNFTBalanceInfoAsync(string nftInfoId)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTBalanceInfo>>(new GraphQLRequest
        {
            Query = @"
			    query($nftInfoId:String!) {
                    data:queryUserBalanceByNftId(dto:{nftInfoId:$nftInfoId}){
                        owner,
                        ownerCount
                    }
                }",
            Variables = new
            {
                nftInfoId
            }
        });
        var result = indexerCommonResult?.Data ?? new IndexerNFTBalanceInfo();
        return result;
    }

    public async Task<IndexerUserMatchedNftIds> GetUserMatchedNftIdsAsync(GetNFTInfosProfileInput input, bool isSeed)
    {
        var indexerCommonResult  = await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerUserMatchedNftIds>>(new GraphQLRequest
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
                    ,$isSeed: Boolean!
                ) {
                data: queryUserNftIdsPage(dto: {
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,nftCollectionId: $nftCollectionId
                ,priceLow: $priceLow
                ,priceHigh: $priceHigh
                ,status: $status
                ,address: $address
                ,issueAddress: $issueAddress
                ,nFTInfoIds: $nftInfoIds
                ,isSeed: $isSeed
                }) {
                        nftIds,
                        count
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
                nftInfoIds = input.NFTInfoIds,
                isSeed
            }
        });
        var result = indexerCommonResult?.Data ?? new IndexerUserMatchedNftIds();
        return result;
    }

    public async Task<UserBalanceIndexerListDto> QueryUserBalanceListAsync(QueryUserBalanceInput input)
    {
        var client = _graphQlClientFactory.GetClient(ClientType);

        var indexerCommonResult = await client.SendQueryAsync<UserBalanceIndexerQuery>(new GraphQLRequest
        {
            Query = 
                @"query($skipCount: Int!,$blockHeight: Long!) {
                    queryUserBalanceList(input: {
                    skipCount: $skipCount
                    ,blockHeight: $blockHeight
                    }) {
                        totalCount,
                        data {
                        id,
                        chainId,
                        blockHeight,
                        address,
                        amount,
                        nFTInfoId,
                        symbol,
                        changeTime,
                        listingPrice,
                        listingTime
                        }
                    }
                }",
            Variables = new
            {
                skipCount = input.SkipCount,
                blockHeight = input.BlockHeight
            }
        });

        return indexerCommonResult?.Data.QueryUserBalanceList;
    }

    public async Task<List<string>> GetNFTIdListByUserBalancesAsync(GetCollectedCollectionActivitiesInput input,
        int skipCount,
        int maxResultCount)
    {
        if (input == null || input.Address.IsNullOrEmpty())
        {
            return null;
        }
        var mustQuery = new List<Func<QueryContainerDescriptor<UserBalanceIndex>, QueryContainer>>();

        if (!input.CollectionIdList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.CollectionId).Terms(input.CollectionIdList)));
        }

        if (!input.ChainList.IsNullOrEmpty())
        {
            mustQuery.Add(q =>
                q.Terms(i => i.Field(f => f.ChainId).Terms(input.ChainList)));
        }
        
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Address).Terms(input.Address)));

        QueryContainer Filter(QueryContainerDescriptor<UserBalanceIndex> f)
            => f.Bool(b => b.Must(mustQuery));

        var result = await _userBalanceIndexRepository.GetListAsync(Filter, sortType: SortOrder.Descending,
            sortExp: item => item.ChangeTime, skip: skipCount, limit: maxResultCount);

        if (result == null)
        {
            return new List<string>();
        }

        return result?.Item2?.Select(item => item.NFTInfoId).ToList();
    }
}