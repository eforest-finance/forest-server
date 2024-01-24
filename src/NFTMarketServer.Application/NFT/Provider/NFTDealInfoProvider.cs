using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public class NFTDealInfoProvider : INFTDealInfoProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;

    public NFTDealInfoProvider(IGraphQLHelper graphQlHelper)
    {
        _graphQlHelper = graphQlHelper;
    }

    public async Task<IndexerNFTDealInfos> GetDealInfosAsync(GetNftDealInfoDto dto)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTDealInfos>(new GraphQLRequest
        {
            Query = @"
			    query($skipCount:Int!,$maxResultCount:Int!,$chainId:String,$symbol:String,$collectionSymbol:String,
                      $sortType:Int!,$sort:String) {
                    data:getNftDealInfos(dto:{
                        skipCount: $skipCount,
                        maxResultCount:$maxResultCount,
                        chainId:$chainId,
                        symbol:$symbol,
                        collectionSymbol:$collectionSymbol,
                        sortType:$sortType,
                        sort:$sort
                    }){
                        totalRecordCount,
                        indexerNftDealList:data{                                           
                             id,chainId,nftFrom,nftTo,nftSymbol,nftQuantity,
                             purchaseSymbol,purchaseTokenId,nftInfoId,purchaseAmount,dealTime,collectionSymbol
                          }
                    }
                }",
            Variables = new
            {
                skipCount = dto.SkipCount,
                maxResultCount = dto.MaxResultCount,
                chainId = dto.ChainId,
                symbol = dto.Symbol,
                collectionSymbol = dto.CollectionSymbol,
                sortType = dto.SortType,
                sort = dto.Sort
            }
        });
        return indexerCommonResult?.Data;
    }
}