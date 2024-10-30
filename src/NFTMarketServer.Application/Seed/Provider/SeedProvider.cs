using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Seed.Provider;

public class SeedProvider : ISeedProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IObjectMapper _objectMapper;

    public SeedProvider(IGraphQLHelper graphQlHelper
        , IObjectMapper objectMapper)
    {
        _graphQlHelper = graphQlHelper;
        _objectMapper = objectMapper;
    }

    public async Task<SeedInfoDto> SearchSeedInfoAsync(SearchSeedInput input)
    {
        var seedInfoDto = await _graphQlHelper.QueryAsync<SeedInfoDto>(new GraphQLRequest
        {
            Query = @"query(
                $symbol: String!
                ,$tokenType: String!
            ){
                data:searchSeedInfo(input:{
                symbol: $symbol
                ,tokenType: $tokenType
                }) {
                   id,
                   symbol,
                   seedSymbol,
                   seedImage,
                   seedName,
                   status,
                   registerTime
                   expireTime
                   tokenType
                   seedType
                   owner
                   currentChainId
                   topBidPrice{
                     symbol
                     amount
                   },
                   notSupportSeedStatus,
                   auctionEndTime
                   tokenPrice{
                     symbol
                     amount
                   }
                 }
            }",
            Variables = new
            {
                symbol = input.Symbol,
                tokenType = input.TokenType
            }
        });
        return seedInfoDto?.Data;
    }

    public async Task<SeedInfoDto> GetSeedInfoAsync(QuerySeedInput input)
    {
        var seedInfoDto = await _graphQlHelper.QueryAsync<SeedInfoDto>(new GraphQLRequest
        {
            Query = @"query(
                $symbol: String!
            ){
                data:getSeedInfo(input:{
                symbol: $symbol
                }){
                   id,
                   symbol,
                   seedSymbol,
                   seedImage,
                   seedName,
                   status
                   registerTime
                   expireTime
                   tokenType
                   seedType
                   owner
                   currentChainId
                   topBidPrice{
                     symbol
                     amount
                   }
                   auctionEndTime
                   tokenPrice{
                     symbol
                     amount
                   }
                }
            }",
            Variables = new
            {
                symbol = input.Symbol
            }
        });
        return seedInfoDto?.Data;
    }

    public async Task<IndexerSpecialSeeds> GetSpecialSeedsAsync(QuerySpecialListInput input)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerSpecialSeeds>(new GraphQLRequest
        {
            Query = @"query(
                $skipCount: Int!
                ,$maxResultCount: Int!
                ,$chainIds: [String]
                ,$isApplyNow: Boolean!
                ,$liveAuction: Boolean!
                ,$symbolLengthMin: Int!
                ,$symbolLengthMax: Int!
                ,$priceMin: Long!
                ,$priceMax: Long!
                ,$tokenTypes:[TokenType!]
                ,$seedTypes:[SeedType!]
            ){
                data: specialSeeds(input:{
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,chainIds: $chainIds
                ,isApplyNow: $isApplyNow
                ,liveAuction: $liveAuction
                ,symbolLengthMin: $symbolLengthMin
                ,symbolLengthMax: $symbolLengthMax
                ,priceMin: $priceMin
                ,priceMax: $priceMax
                ,tokenTypes: $tokenTypes
                ,seedTypes: $seedTypes}){
                        totalRecordCount
                        indexerSpecialSeedList:data {
                           symbol,
                           seedSymbol,
                           seedImage,
                           seedName,
                           status,
                           tokenType,
                           seedType,
                           tokenPrice{
                             symbol,
                             amount
                           }
                        }
                }
            }",
            Variables = new
            {
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                chainIds = input.ChainIds,
                isApplyNow = input.IsApplyNow,
                liveAuction = input.LiveAuction,
                symbolLengthMin = input.SymbolLengthMin,
                symbolLengthMax = input.SymbolLengthMax,
                priceMin = input.PriceMin,
                priceMax = input.PriceMax,
                tokenTypes = input.TokenTypes,
                seedTypes = input.SeedTypes
            }
        });
        return indexerCommonResult?.Data;
    }
}