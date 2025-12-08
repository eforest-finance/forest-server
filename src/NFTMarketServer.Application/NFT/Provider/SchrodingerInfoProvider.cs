using System;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using Volo.Abp.DependencyInjection;
using GraphQL;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.Contracts.HandleException;
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
    [ExceptionHandler(typeof(Exception),
        Message = "SchrodingerInfoProvider.GetSchrodingerInfoAsync Indexer error", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRetrun),
        ReturnDefault = ReturnDefault.New,
        LogTargets = new []{"input"}
    )]
    public virtual async Task<SchrodingerSymbolIndexerListDto> GetSchrodingerInfoAsync(GetCatListInput input)
    {
        var client = _graphQlClientFactory.GetClient(ClientType);
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
}