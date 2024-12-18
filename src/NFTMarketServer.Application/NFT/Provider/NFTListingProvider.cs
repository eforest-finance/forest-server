using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using AElf.ExceptionHandler;
using GraphQL;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using NFTMarketServer.Common;
using NFTMarketServer.HandleException;
using NFTMarketServer.Market;
using NFTMarketServer.NFT.Index;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public interface INFTListingProvider
{
    Task<PagedResultDto<IndexerNFTListingInfo>> GetNFTListingsAsync(GetNFTListingsDto dto);

    Task<PagedResultDto<IndexerNFTListingInfo>> GetAllNFTListingsByHeightAsync(GetNFTListingsDto dto);

    Task<IndexerNFTListingInfos> GetCollectedNFTListingsAsync(int skipCount, int maxResultCount,
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

public class NFTListingAllPage : IndexerCommonResult<NFTListingPage>
{
    public PagedResultDto<IndexerNFTListingInfo> nftListingInfoAll { get; set; }
    
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

    [ExceptionHandler(typeof(Exception),
        Message = "NFTListingProvider.GetNFTListingsAsync query GraphQL error dto", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"dto"}
    )]
    public virtual async Task<PagedResultDto<IndexerNFTListingInfo>> GetNFTListingsAsync(GetNFTListingsDto dto)
    {
       var res = await _graphQlHelper.QueryAsync<NFTListingPage>(new GraphQLRequest
            {
                Query = @"query (
                    $skipCount:Int!,
                    $maxResultCount:Int!,
                    $chainId:String!,
                    $symbol:String!,
                    $owner:String,
                    $address:String,
                    $excludedAddress:String,
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
    [ExceptionHandler(typeof(Exception),
        Message = "NFTListingProvider.GetCollectedNFTListingsAsync query GraphQL error", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"skipCount","maxResultCount","owner","chainIdList","nftInfoIdList"}
    )]
    public virtual async Task<IndexerNFTListingInfos> GetCollectedNFTListingsAsync(int skipCount, int maxResultCount, string owner, List<string> chainIdList,
        List<string> nftInfoIdList)
    {
        var indexerCommonResult = await _graphQlHelper.QueryAsync<IndexerNFTListingInfos>(new GraphQLRequest
        {
            Query = @"query (
                    $skipCount:Int!,
                    $maxResultCount:Int!,
                    $chainIdList:[String!]!,
                    $nFTInfoIdList:[String!]!,
                    $owner:String!,
                    $expireTimeGt:Long
                ){
                  data:collectedNFTListingInfo(
                    dto:{
                      skipCount:$skipCount,
                      maxResultCount:$maxResultCount,
                      chainIdList:$chainIdList,
                      nFTInfoIdList:$nFTInfoIdList,
                      owner:$owner,
                      expireTimeGt:$expireTimeGt
                    }
                  ){
                    totalRecordCount,
                    indexerNFTListingInfoList:data{
                      id,
                      businessId,
                      quantity,
                      realQuantity,
                      symbol,
                      owner,
                      prices,
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
        return indexerCommonResult?.Data;

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
			    query($skipCount:Int!,$chainId:String!,$startBlockHeight:Long!) {
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
    [ExceptionHandler(typeof(Exception),
        Message = "NFTListingProvider.GetAllNFTListingsByHeightAsync query GraphQL error dto", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionRethrow),
        LogTargets = new []{"dto"}
    )]
    public virtual async Task<PagedResultDto<IndexerNFTListingInfo>> GetAllNFTListingsByHeightAsync(GetNFTListingsDto dto)
    {
        var res = await _graphQlHelper.QueryAsync<NFTListingAllPage>(new GraphQLRequest
            {
                Query = @"query (
                    $skipCount:Int!,
                    $maxResultCount:Int!,
                    $chainId:String!,
                    $symbol:String!,
                    $owner: String,
                    $address: String,
                    $excludedAddress: String,
                    $expireTimeGt:Long,
                    $blockHeight:Long
                ){
                  nftListingInfoAll(
                    input:{
                      skipCount:$skipCount,
                      maxResultCount:$maxResultCount,
                      chainId:$chainId,
                      symbol:$symbol,
                      owner:$owner,
                      address:$address,
                      excludedAddress:$excludedAddress,
                      expireTimeGt:$expireTimeGt,
                      blockHeight:$blockHeight
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
                    skipCount = dto.SkipCount, 
                    maxResultCount = dto.MaxResultCount, 
                    expireTimeGt = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                    blockHeight = dto.BlockHeight
                }
            });
            return res?.nftListingInfoAll;
    }
}