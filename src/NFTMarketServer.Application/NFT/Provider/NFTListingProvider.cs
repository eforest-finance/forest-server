using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface INFTListingProvider
{
    Task<PagedResultDto<IndexerNFTListingInfo>> GetNFTListingsAsync(GetNFTListingsDto dto);

    Task<PagedResultDto<IndexerNFTListingInfo>> GetCollectedNFTListingsAsync(int skipCount, int maxResultCount,
        string owner, List<string> chainIdList, List<string> nftInfoIdList);
    
    Task<IndexerNFTListingInfo> GetMinListingNftAsync(string nftInfoId);

    Task<List<IndexerNFTListingInfoResult>> GetExpiredListingNftAsync(string chainId, long expireTimeGt);
    
    Task<List<ExpiredNftMinPriceDto>> GetNftMinPriceAsync(string chainId, long expiredSecond);
    
    public Task<IndexerNFTListingChangePage> GetIndexerNFTListingChangePageByBlockHeightAsync(int skipCount,
        string chainId, long startBlockHeight);

}

public class NFTListingPage : IndexerCommonResult<NFTListingPage>
{
    public PagedResultDto<IndexerNFTListingInfo> nftListingInfo { get; set; }
    
}

public class NFTListingProvider : INFTListingProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    
    private readonly ILogger<NFTListingProvider> _logger;

    public NFTListingProvider(IGraphQLHelper graphQlHelper, ILogger<NFTListingProvider> logger)
    {
        _graphQlHelper = graphQlHelper;
        _logger = logger;
    }


    public async Task<PagedResultDto<IndexerNFTListingInfo>> GetNFTListingsAsync(GetNFTListingsDto dto)
    {
        try
        {

            var res = await _graphQlHelper.QueryAsync<NFTListingPage>(new GraphQLRequest
            {
                Query = @"query (
                    $skipCount:Int!,
                    $maxResultCount:Int!,
                    $chainId:String,
                    $symbol:String,
                    $owner: String,
                    $address: String,
                    $excludedAddress: String,
                    $expireTimeGt:Long
                ){
                  nftListingInfo(
                    input:{
                      skipCount:$skipCount,
                      maxResultCount:$maxResultCount,
                      chainId:$chainId,
                      symbol:$symbol,
                      owner:$owner,
                      address:$address,
                      excludedAddress:$excludedAddress,
                      expireTimeGt:$expireTimeGt
                    }
                  ){
                    TotalCount: totalRecordCount,
                    Message: message,
                    Items: data{
                      id,
                      quantity,
                      realQuantity,
                      symbol,
                      owner,
                      prices,
                      whitelistPrices,
                      whitelistId,
                      startTime,
                      publicTime,
                      expireTime,
                      chainId,
                      purchaseToken {
      	                chainId,symbol,tokenName,
                      }
                    }
                  }
                }",
                Variables = new
                {
                    chainId = dto.ChainId, 
                    symbol = dto.Symbol, 
                    owner = dto.Address,
                    address = dto.Address,
                    excludedAddress = dto.ExcludedAddress,
                    skipCount = dto.SkipCount, 
                    maxResultCount = dto.MaxResultCount, 
                    expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
                }
            });
            return res?.nftListingInfo;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetNFTListingsAsync query GraphQL error dto={DTO}", JsonConvert.SerializeObject(dto));
            throw;
        }
    }

    public async Task<PagedResultDto<IndexerNFTListingInfo>> GetCollectedNFTListingsAsync(int skipCount, int maxResultCount, string owner, List<string> chainIdList,
        List<string> nftInfoIdList)
    {
        try
        {
            var res = await _graphQlHelper.QueryAsync<NFTListingPage>(new GraphQLRequest
            {
                Query = @"query (
                    $skipCount:Int!,
                    $maxResultCount:Int!,
                    $chainIdList:[String],
                    $nFTInfoIdList:[String],
                    $owner:String!,
                    $expireTimeGt:Long
                ){
                  collectedNFTListingInfo(
                    dto:{
                      skipCount:$skipCount,
                      maxResultCount:$maxResultCount,
                      chainIdList:$chainIdList,
                      nFTInfoIdList:$nFTInfoIdList,
                      owner:$owner,
                      expireTimeGt:$expireTimeGt
                    }
                  ){
                    TotalCount: totalRecordCount,
                    Message: message,
                    nftListingInfo: data{
                      id,
                      businessId,
                      quantity,
                      realQuantity,
                      symbol,
                      owner,
                      prices,
                      whitelistPrices,
                      whitelistId,
                      startTime,
                      publicTime,
                      expireTime,
                      chainId,
                      purchaseToken {
      	                chainId,symbol,tokenName,
                      }
                    }
                  }
                }",
                Variables = new
                {
                    chainIdList = chainIdList, 
                    nFTInfoIdList = nftInfoIdList,
                    owner = owner,
                    skipCount = skipCount, 
                    maxResultCount = maxResultCount, 
                    expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
                }
            });
            return res?.nftListingInfo;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetCollectedNFTListingsAsync query GraphQL error owner={A} chainIdList={B} nftInfoIdList={C}",
                owner, JsonConvert.SerializeObject(chainIdList), JsonConvert.SerializeObject(nftInfoIdList));
            throw;
        }
    }

    public async Task<IndexerNFTListingInfo> GetMinListingNftAsync(string nftInfoId)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTListingInfo>(new GraphQLRequest
        {
            Query = @"
			    query($nftInfoId:String!) {
                    getMinListingNft(dto:{nftInfoId:$nftInfoId}){
                      id,
                      quantity,
                      symbol,
                      owner,
                      prices,
                      whitelistPrices,
                      whitelistId,
                      startTime,
                      publicTime,
                      expireTime,
                      chainId,
                      purchaseToken {
      	                chainId,symbol,tokenName,
                      }
                    }
                }",
            Variables = new
            {
                nftInfoId
            }
        });
        return indexerCommonResult;
    }

    public async Task<List<IndexerNFTListingInfoResult>> GetExpiredListingNftAsync(string chainId, long expireTimeGt)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<IndexerNFTListingInfoResultDto>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!, $expireTimeGt:Long!) 
                {
                    getExpiredListingNft(dto:
                    {
                      chainId: $chainId,
                      expireTimeGt: $expireTimeGt
                    }){
                        chainId,
                        symbol,
                        nftInfoId,
                        collectionSymbol,
                        prices,
                        expireTime
                    }
                }",
            Variables = new
            {
                chainId,
                expireTimeGt
            }
        });
        if (graphQlResponse == default(IndexerNFTListingInfoResultDto))
        {
            return new List<IndexerNFTListingInfoResult>();
        }
        return graphQlResponse.GetExpiredListingNft.IsNullOrEmpty()
            ? new List<IndexerNFTListingInfoResult>()
            : graphQlResponse.GetExpiredListingNft;
    }
    
    public async Task<List<ExpiredNftMinPriceDto>> GetNftMinPriceAsync(string chainId, long expiredSecond)
    {
        var graphQlResponse = await _graphQlHelper.QueryAsync<ExpiredNftMinPriceResultDto>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String!, $expireTimeGt:Long!) 
                {
                    getExpiredNftMinPrice(input:
                    {
                      chainId: $chainId,
                      expireTimeGt: $expireTimeGt
                    }){
                         key,
                         value 
                         {
                             id,
                             expireTime,
                             prices,
                             symbol     
                         }
                    }
                }",
            Variables = new
            {
                chainId = chainId, 
                expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddSeconds(-expiredSecond))
            }
        });
        if (graphQlResponse == default(ExpiredNftMinPriceResultDto))
        {
            return new List<ExpiredNftMinPriceDto>();
        }

        return graphQlResponse.GetExpiredNftMinPrice.IsNullOrEmpty()
            ? new List<ExpiredNftMinPriceDto>()
            : graphQlResponse.GetExpiredNftMinPrice;
    }

    public async Task<IndexerNFTListingChangePage> GetIndexerNFTListingChangePageByBlockHeightAsync(int skipCount, string chainId, long startBlockHeight)
    {
        var indexerCommonResult =
            await _graphQlHelper.QueryAsync<IndexerCommonResult<IndexerNFTListingChangePage>>(new GraphQLRequest
            {
                Query = @"
			    query($skipCount:Int!,$chainId:String,$startBlockHeight:Long!) {
                    data:nftListingChange(dto:{skipCount:$skipCount,chainId:$chainId,blockHeight:$startBlockHeight}) {
                        totalRecordCount,
                        indexerNFTListingChangeList:data{
                             chainId,
                             symbol,
                             blockHeight
                        }
                    }
                  }",
                Variables = new
                {
                    skipCount,
                    chainId,
                    startBlockHeight
                }
            });
        return indexerCommonResult?.Data;
    }
}