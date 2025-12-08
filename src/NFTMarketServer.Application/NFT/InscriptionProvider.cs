using System.Linq;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT;

public interface IInscriptionProvider
{
    public Task<InscriptionInfoDto> GetIndexerInscriptionInfoAsync(string chainId, string tick);
}

public class InscriptionProvider : IInscriptionProvider, ISingletonDependency
{
    private readonly IGraphQLClientFactory _graphQlClientFactory;
    private readonly ILogger<InscriptionProvider> _logger;
    
    public InscriptionProvider(ILogger<InscriptionProvider> logger, IGraphQLClientFactory graphQlClientFactory)
    {
        _logger = logger;
        _graphQlClientFactory = graphQlClientFactory;
    }
    
    public async Task<InscriptionInfoDto> GetIndexerInscriptionInfoAsync(string chainId, string tick)
    {
        var issuedInscriptionResult =  await _graphQlClientFactory.GetClient(GraphQLClientEnum.InscriptionClient).SendQueryAsync<InscriptionInfoDtoPageInfo>(new GraphQLRequest
        {
            Query = @"query(
                    $chainId: String,
                    $tick: String,
                    $skipCount : Int!,
                    $maxResultCount : Int!
                ) {
                data: issuedInscription(input: {
                    chainId:$chainId,
                    tick:$tick,
                    skipCount:$skipCount,
                    maxResultCount:$maxResultCount
                }) {
                        totalRecordCount:totalCount
                        inscriptionInfoDtoList:items {
                           tick
                           issuedTransactionId
                           deployTime:issuedTime
                        }
                    }
                }",
                Variables = new
                {
                    chainId = chainId,
                    tick = tick,
                    skipCount = 0,
                    maxResultCount = 1
                }
        });
        var inscriptionInfoDto = issuedInscriptionResult?.Data?.Data?.InscriptionInfoDtoList?.FirstOrDefault();
        if (inscriptionInfoDto == null)
        {
            return null;
        }
        
        var inscriptionResult =  await _graphQlClientFactory.GetClient(GraphQLClientEnum.InscriptionClient).SendQueryAsync<InscriptionInfoDtos>(new GraphQLRequest
        {
            Query = @"query(
                    $chainId: String,
                    $tick: String
                ) {
                inscription(input: {
                    chainId:$chainId,
                    tick:$tick
                }) {
                      mintLimit:limit
                    }
                }",
            Variables = new
            {
                tick = tick,
                chainId = SymbolHelper.MAIN_CHAIN_SYMBOL

            }
        });

        var inscription = inscriptionResult?.Data?.Inscription?.FirstOrDefault();
        if (inscription == null)
        {
            return inscriptionInfoDto;
        }
        
        inscriptionInfoDto.MintLimit = inscription.MintLimit;
        return inscriptionInfoDto;
    }
    
}