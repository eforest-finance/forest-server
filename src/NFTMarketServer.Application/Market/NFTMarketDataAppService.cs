using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Market;

[RemoteService(IsEnabled = false)]
public class NFTMarketDataAppService : NFTMarketServerAppService, INFTMarketDataAppService
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly IObjectMapper _objectMapper;

    public NFTMarketDataAppService(IGraphQLHelper graphQlHelper, IObjectMapper objectMapper)
    {
        _graphQlHelper = graphQlHelper;
        _objectMapper = objectMapper;
    }

    public async Task<ListResultDto<NFTInfoMarketDataDto>> GetMarketDataAsync(GetNFTInfoMarketDataInput input)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTInfoMarketDatas>(new GraphQLRequest
        {
            Query = @"
			 		    query ($skipCount:Int!,$maxResultCount:Int!,$nFTInfoId:String,$timestampMin:Long!,$timestampMax:Long!) {
                  data:marketData(input:{skipCount: $skipCount
                                    ,maxResultCount:$maxResultCount
                                    ,nFTInfoId:$nFTInfoId
                                    ,timestampMin:$timestampMin
                                    ,timestampMax:$timestampMax}){
                                    totalRecordCount,
                                    indexerNftInfoMarketDatas:data{
                                     price,
                                     timestamp
                                    }
                    }
                }",
            Variables = new
            {
                skipCount = input.SkipCount, maxResultCount = input.MaxResultCount, nFTInfoId = input.NFTInfoId,
                timestampMin = input.TimestampMin,
                timestampMax = input.TimestampMax
            }
        });
        var queryResult = indexerCommonResult?.Data?.indexerNftInfoMarketDatas;
        return new ListResultDto<NFTInfoMarketDataDto>
        {
            Items = _objectMapper.Map<List<IndexerNFTInfoMarketData>, List<NFTInfoMarketDataDto>>(queryResult)
        };
    }
}