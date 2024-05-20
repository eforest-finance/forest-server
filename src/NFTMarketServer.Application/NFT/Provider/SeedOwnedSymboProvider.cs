using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public class SeedOwnedSymboProvider : ISeedOwnedSymboProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;

    public SeedOwnedSymboProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }
    public async Task<IndexerSeedOwnedSymbols> GetSeedOwnedSymbolsIndexAsync(long inputSkipCount,
        long inputMaxResultCount, string inputAddress, string inputSeedOwnedSymbol)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerSeedOwnedSymbols>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$address:String,$seedOwnedSymbol:String) {
                    data:seedSymbols(dto:{skipCount: $skipCount,maxResultCount:$maxResultCount
                    ,address:$address,seedOwnedSymbol:$seedOwnedSymbol}){
                        totalRecordCount,
                        indexerSeedOwnedSymbolList:data{
                                   id,
                                   issuer,
                                   isBurnable,
                                    createTime,
                                    seedSymbol:symbol,
                                    symbol:seedOwnedSymbol,
                                    seedExpTimeSecond,
                                    seedExpTime       
                                    }
                    }
                }",
            Variables = new
            {
                skipCount = inputSkipCount,
                maxResultCount = inputMaxResultCount,
                address = inputAddress,
                seedOwnedSymbol = inputSeedOwnedSymbol
            }
        });
        return indexerCommonResult?.Data;
    }
    
    public async Task<IndexerSeedOwnedSymbols> GetAllSeedOwnedSymbolsIndexAsync(long inputSkipCount,
        long inputMaxResultCount, List<string> inputAddressList, string inputSeedOwnedSymbol)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerSeedOwnedSymbols>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$addressList:[String]!,$seedOwnedSymbol:String) {
                    data:allSeedSymbols(dto:{skipCount: $skipCount,maxResultCount:$maxResultCount
                    ,addressList:$addressList,seedOwnedSymbol:$seedOwnedSymbol}){
                        totalRecordCount,
                        indexerSeedOwnedSymbolList:data{
                                   id,
                                   issuer,
                                   isBurnable,
                                    createTime,
                                    seedSymbol:symbol,
                                    symbol:seedOwnedSymbol,
                                    seedExpTimeSecond,
                                    seedExpTime,     
                                    chainId
                                    }
                    }
                }",
            Variables = new
            {
                skipCount = inputSkipCount,
                maxResultCount = inputMaxResultCount,
                addressList = inputAddressList,
                seedOwnedSymbol = inputSeedOwnedSymbol
            }
        });
        return indexerCommonResult?.Data;
    }
}