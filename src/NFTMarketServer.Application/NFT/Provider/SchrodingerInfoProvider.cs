using System;
using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;
using GraphQL;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Dtos;

namespace NFTMarketServer.NFT.Provider;

public class SchrodingerInfoProvider : ISchrodingerInfoProvider, ISingletonDependency
{
    private readonly IGraphQLClientFactory _graphQlClientFactory;
    private const GraphQLClientEnum ClientType = GraphQLClientEnum.SchrodingerClient;
    private readonly ILogger<SchrodingerInfoProvider> _logger;

    public SchrodingerInfoProvider(IGraphQLClientFactory graphQlClientFactory,ILogger<SchrodingerInfoProvider> logger)
    {
        _graphQlClientFactory = graphQlClientFactory;
        _logger = logger;
    }

    public async Task<SchrodingerSymbolIndexerListDto> GetSchrodingerInfoAsync(GetCatListInput input)
    {
        var client = _graphQlClientFactory.GetClient(ClientType);

        try
        {
            var indexerResult = await client.SendQueryAsync<SchrodingerSymbolIndexerQuery>(new GraphQLRequest
            {
                Query =
                    @"query($keyword:String!, $chainId:String!, $tick:String!, $traits:[TraitsInput!],$raritys:[String!],$generations:[Int!],$skipCount:Int!,$maxResultCount:Int!,$filterSgr:Boolean!){
                    getAllSchrodingerList(input: {keyword:$keyword,chainId:$chainId,tick:$tick,traits:$traits,raritys:$raritys,generations:$generations,skipCount:$skipCount,maxResultCount:$maxResultCount,filterSgr:$filterSgr}){
                        totalCount,
                        data{
                        symbol,
                        tokenName,
                        inscriptionImageUri,
                        amount,
                        generation,
                        decimals,
                        inscriptionDeploy,
                        adopter,
                        adoptTime,
                        traits{traitType,value},
                        rarity,
                        rank,
                        level,
                        grade
                    }
                }
            }",
                Variables = new
                {
                    keyword = input.Keyword ?? "", chainId = input.ChainId ?? "", tick = "",
                    skipCount = input.SkipCount, maxResultCount = input.MaxResultCount,filterSgr = input.FilterSgr
                }
            });

            return indexerResult?.Data.GetAllSchrodingerList;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetSchrodingerInfoAsync Indexer error");
            return new SchrodingerSymbolIndexerListDto();
        }
    }
}