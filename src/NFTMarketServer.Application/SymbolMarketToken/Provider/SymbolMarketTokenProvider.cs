using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.SymbolMarketToken.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.SymbolMarketToken.Provider;

public class SymbolMarketTokenProvider : ISymbolMarketTokenProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IObjectMapper _objectMapper;
    private ISymbolMarketTokenProvider _symbolMarketTokenProviderImplementation;

    public SymbolMarketTokenProvider(IGraphQLHelper graphQlHelper
        , IObjectMapper objectMapper)
    {
        _graphQlHelper = graphQlHelper;
        _objectMapper = objectMapper;
    }
    
    public async Task<IndexerSymbolMarketTokens> GetSymbolMarketTokenAsync(List<string> address, long skipCount,
        long maxResultCount)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerSymbolMarketTokens>(new GraphQLRequest
        {
            Query = @"query(
                $skipCount: Int!
                ,$maxResultCount: Int!
                ,$address: [String]
            ){
                data: symbolMarketTokens(dto:{
                skipCount: $skipCount
                ,maxResultCount: $maxResultCount
                ,address: $address}){
                        totalRecordCount
                        indexerSymbolMarketTokenList:data{
                           symbolMarketTokenLogoImage,
                           symbol,
                           tokenName,
                           issuer,
                           decimals,
                           totalSupply,
                           supply,
                           issued,
                           issueChainId,
                           issueManagerList
                        }
                }
            }",
            Variables = new
            {
                address = address,
                skipCount = skipCount,
                maxResultCount = maxResultCount
            }
        });
        return indexerCommonResult?.Data;
    }

    public async Task<IndexerSymbolMarketIssuer> GetSymbolMarketTokenIssuerAsync(int issueChainId, string tokenSymbol)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerSymbolMarketIssuer>(new GraphQLRequest
        {
            Query = @"query(
                $issueChainId: Int!
                ,$tokenSymbol: String!
            ){
                data: symbolMarketTokenIssuer(dto:{
                issueChainId: $issueChainId
                ,tokenSymbol: $tokenSymbol}){
                        symbolMarketTokenIssuer
                       }
            }",
            Variables = new
            {
                issueChainId = issueChainId,
                tokenSymbol = tokenSymbol
            }
        });
        return indexerCommonResult.Data;
    }
}