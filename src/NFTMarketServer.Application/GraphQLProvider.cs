using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NFTMarketServer.Basic;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Chain;
using NFTMarketServer.Common;
using NFTMarketServer.Common.AElfSdk.Dtos;
using NFTMarketServer.Grains.ApplicationHandler;
using NFTMarketServer.Inscription;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Orleans;
using Volo.Abp.DependencyInjection;
using TokenType = NFTMarketServer.Seed.Dto.TokenType;
using NFTMarketServer.Common.Http;
using NFTMarketServer.Contracts.HandleException;

namespace NFTMarketServer;

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly GraphQLHttpClient _graphQLClient;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly IGraphQLClientFactory _graphQlClientFactory;
    private readonly IHttpService _httpService;
    private readonly IOptionsMonitor<GraphQLOptions> _graphQLOptions;


    public GraphQLProvider(ILogger<GraphQLProvider> logger, IClusterClient clusterClient,
        IOptionsMonitor<GraphQLOptions> graphQLOptions, IGraphQLClientFactory graphQlClientFactory,
        IHttpService httpService)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _graphQLOptions = graphQLOptions;
        _graphQLClient = new GraphQLHttpClient(_graphQLOptions.CurrentValue.Configuration, new NewtonsoftJsonSerializer());
        _graphQlClientFactory = graphQlClientFactory;
        _httpService = httpService;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "GraphQLProvider.GetLastEndHeightAsync:GetIndexBlockHeight on chain error", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGraphQLRetrun),
        LogTargets = new []{"chainId", "queryChainType" }
    )]
    public virtual async Task<long> GetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType)
    {
        var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() + chainId);
        return await grain.GetStateAsync();
    }

    [ExceptionHandler(typeof(Exception),
        Message = "GraphQLProvider.SetLastEndHeightAsync:SetIndexBlockHeight on chain error", 
        TargetType = typeof(ExceptionHandlingService), 
        MethodName = nameof(ExceptionHandlingService.HandleExceptionGraphQLRetrun),
        LogTargets = new []{"chainId", "queryChainType", "height" }
    )]
    public virtual async Task SetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType, long height)
    {
        var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() +
                                                                          chainId);
        await grain.SetStateAsync(height);
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId, BusinessQueryChainType queryChainType)
    {
        var result = new AelfScanTokenAppResponse();

        var syncStateUrl = queryChainType == BusinessQueryChainType.InscriptionCrossChain
            ? _graphQLOptions.CurrentValue.InscriptionBasicConfiguration
            : _graphQLOptions.CurrentValue
                .BasicConfiguration;
        
        var resultStr = await _httpService.SendGetRequest(syncStateUrl,
            new Dictionary<string, string>());
        if (resultStr.IsNullOrEmpty()) return 0;
        result = JsonConvert.DeserializeObject<AelfScanTokenAppResponse>(resultStr);
        if (result != null && result.CurrentVersion != null && !result.CurrentVersion.Items.IsNullOrEmpty())
        {
            var list = result.CurrentVersion.Items;
            var first = list.FirstOrDefault(item => item.ChainId == chainId);
            if (first == null)
            {
                return 0;
            }

            return first.LastIrreversibleBlockHeight;
        }

        return 0;
    }

    public async Task<List<AuctionInfoDto>> GetSyncSymbolAuctionRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<SymbolAuctionRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!){
            getSymbolAuctionInfos(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                id,
                seedSymbol:symbol,
                startPrice 
                {
                    symbol,
                    amount
                },
                startTime,
                endTime,
                maxEndTime,
                minMarkup,
                blockHeight,
                finishIdentifier,
                finishBidder,
                finishTime,
                finishPrice
                {
                    symbol,
                    amount
                },
                receivingAddress,
                duration,
                creator,
                collectionSymbol,
                transactionHash
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        return graphQlResponse.Data.GetSymbolAuctionInfos.IsNullOrEmpty() ? new List<AuctionInfoDto>() : graphQlResponse.Data.GetSymbolAuctionInfos;
    }
    
    
    public async Task<List<SeedDto>> GetSyncTsmSeedRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var str = new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!){
            seedDtoList:getTsmSeedInfos(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                id,
                chainId,
				symbol,	
				seedSymbol,
       		    seedImage,
                seedName,
                blockHeight,
                status,
                registerTime,
                expireTime,
                tokenType,
                seedType,
                auctionType,
                owner,
                tokenPrice{
                    symbol,
                    amount
                },
                isBurned,
                auctionStatus,
                bidsCount,
                biddersCount,
                auctionEndTime,
                topBidPrice{
                    symbol,
                    amount
                }
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        };
        var graphQlResponse = await _graphQLClient.SendQueryAsync<MySeedDto>(str);
        return graphQlResponse.Data.SeedDtoList.IsNullOrEmpty()
            ? new List<SeedDto>()
            : graphQlResponse.Data.SeedDtoList;
    }

    public async Task<List<SeedPriceDto>> GetSeedPriceDtoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<SeedPriceRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!){
            getSeedPriceInfos(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                id,
                tokenType,
                tokenPrice 
                {
                    symbol,
                    amount
                },
                symbolLength,
                blockHeight
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        return graphQlResponse.Data.GetSeedPriceInfos.IsNullOrEmpty() ? new List<SeedPriceDto>() : graphQlResponse.Data.GetSeedPriceInfos;
    }

    public async Task<List<UniqueSeedPriceDto>> GetUniqueSeedPriceDtoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<UniqueSeedPriceRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!){
            getUniqueSeedPriceInfos(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                id,
                tokenType,
                tokenPrice 
                {
                    symbol,
                    amount
                },
                symbolLength,
                blockHeight
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        return graphQlResponse.Data.GetUniqueSeedPriceInfos.IsNullOrEmpty() ? new List<UniqueSeedPriceDto>() : graphQlResponse.Data.GetUniqueSeedPriceInfos;
    }

    public async Task<List<NFTInfoIndex>> GetSyncNftInfoRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<IndexerNFTInfoSync>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!){
            dataList:getSyncNftInfoRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},
                issueManagerSet,randomIssueManager,creatorAddress,imageUrl,collectionSymbol,collectionName,collectionId,otherOwnerListingFlag,
                listingId,listingAddress,listingPrice,listingQuantity,listingEndTime,latestListingTime,latestOfferTime,latestDealPrice,latestDealTime,offerPrice,offerQuantity,offerExpireTime,
                offerToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                listingToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                latestDealToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                whitelistPriceToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                previewImage,file,fileExtension,description,isOfficial,hasListingFlag,minListingPrice,minListingExpireTime,minListingId,hasOfferFlag,maxOfferPrice
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        return graphQlResponse.Data.DataList.IsNullOrEmpty() ? new List<NFTInfoIndex>() : graphQlResponse.Data.DataList;
    }

    public async Task<NFTInfoIndex> GetSyncNftInfoRecordAsync(string id, string chainId)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<JObject>(new GraphQLRequest
        {
            Query =
                @"query($id:String!,$chainId:String!){
            getSyncNFTInfoRecord(dto: {id:$id,chainId:$chainId})
            {
                id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},
                issueManagerSet,randomIssueManager,creatorAddress,imageUrl,collectionSymbol,collectionName,collectionId,otherOwnerListingFlag,
                listingId,listingAddress,listingPrice,listingQuantity,listingEndTime,latestListingTime,latestOfferTime,latestDealPrice,latestDealTime,offerPrice,offerQuantity,offerExpireTime,
                offerToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                listingToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                latestDealToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                whitelistPriceToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                previewImage,file,fileExtension,description,isOfficial,hasListingFlag,minListingPrice,minListingExpireTime,minListingId,hasOfferFlag,maxOfferPrice
            }}",
            Variables = new
            {
                id,
                chainId
            }
            
        });
        var responseData = graphQlResponse.Data;
        if (responseData != null)
        {
            var nftInfoRecord = responseData[CommonConstant.Graphql_Method].ToObject<NFTInfoIndex>();
            return nftInfoRecord;
        }

        return null;
    }

    public async Task<List<SeedSymbolIndex>> GetSyncSeedSymbolRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<IndexerSeedSymbolSync>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!){
             dataList:getSyncSeedSymbolRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},
                seedOwnedSymbol,seedExpTimeSecond,seedExpTime,registerTimeSecond,registerTime,issuerTo,isDeleteFlag,tokenType,seedType,price,priceSymbol,
                beginAuctionPrice,auctionPrice,auctionPrice,auctionPriceSymbol,auctionDateTime,otherOwnerListingFlag,listingId,listingAddress,listingPrice,listingQuantity,listingEndTime,latestListingTime,
                offerPrice,offerQuantity,offerExpireTime,latestOfferTime,
                offerToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                listingToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                latestDealToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                seedStatus,hasOfferFlag,hasListingFlag,minListingPrice,minListingExpireTime,minListingId,hasAuctionFlag,maxAuctionPrice,maxOfferPrice,seedImage                
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        return graphQlResponse.Data.DataList.IsNullOrEmpty() ? new List<SeedSymbolIndex>() : graphQlResponse.Data.DataList;
    }
    
    public async Task<SeedSymbolIndex> GetSyncSeedSymbolRecordAsync(string id, string chainId)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<JObject>(new GraphQLRequest
        {
            Query =
                @"query($id:String!,$chainId:String!){
            getSyncSeedSymbolRecord(dto: {id:$id,chainId:$chainId})
            {
                id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},
                seedOwnedSymbol,seedExpTimeSecond,seedExpTime,registerTimeSecond,registerTime,issuerTo,isDeleteFlag,tokenType,seedType,price,priceSymbol,
                beginAuctionPrice,auctionPrice,auctionPrice,auctionPriceSymbol,auctionDateTime,otherOwnerListingFlag,listingId,listingAddress,listingPrice,listingQuantity,listingEndTime,latestListingTime,
                offerPrice,offerQuantity,offerExpireTime,latestOfferTime,
                offerToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                listingToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                latestDealToken{id,chainId,blockHeight,symbol,tokenContractAddress,decimals,supply,totalSupply,tokenName,owner,issuer,isBurnable,issueChainId,issued,createTime,externalInfoDictionary{key, value},prices},
                seedStatus,hasOfferFlag,hasListingFlag,minListingPrice,minListingExpireTime,minListingId,hasAuctionFlag,maxAuctionPrice,maxOfferPrice,seedImage                
            }}",
            Variables = new
            {
                id,
                chainId
            }
            
        });
        var responseData = graphQlResponse.Data;
        if (responseData != null)
        {
            var nftInfoRecord = responseData[CommonConstant.GraphqlMethodGetSyncSeedSymbolRecord].ToObject<SeedSymbolIndex>();
            return nftInfoRecord;
        }

        return null;
    }
    

    public async Task<List<BidInfoDto>> GetSyncSymbolBidRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<SymbolBidRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String!,$startBlockHeight:Long!,$endBlockHeight:Long!){
            getSymbolBidInfos(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                id,
                seedSymbol:symbol,
                bidder,
                priceAmount,
                priceSymbol,
                bidTime,
                blockHeight,
                auctionId,
                transactionHash
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        return graphQlResponse.Data.GetSymbolBidInfos.IsNullOrEmpty() ? new List<BidInfoDto>() : graphQlResponse.Data.GetSymbolBidInfos;
    }

    public async Task<SeedInfoDto> GetSeedInfoAsync(string symbol)
    {
        
        var result = await _graphQLClient.SendQueryAsync<SearchSeedInfoResultDto>(new GraphQLRequest
        {
            Query = @"query(
                $symbol: String!,
                $tokenType: TokenType!
            ){
                searchSeedInfo(input:{
                symbol: $symbol
                ,tokenType: $tokenType
                }){
                   id,
                   symbol,
                   seedSymbol,
                   seedName,
                   status,
                   registerTime,
                   expireTime,
                   tokenType,
                   seedType,
                   tokenPrice{
                     symbol
                     amount
                   }
                }
            }",
            Variables = new
            {
                symbol,
                tokenType = symbol.Contains(NFTSymbolBasicConstants.NFTSymbolSeparator) ? TokenType.NFT : TokenType.FT
            }
        });
        return result.Data?.SearchSeedInfo ?? new SeedInfoDto();
    }

    public async Task<List<InscriptionDto>> GetIndexInscriptionAsync(string chainId, long beginBlockHeight,
        long endBlockHeight, int skipCount, int maxResultCount)
    {
        var graphQLResponse = await _graphQlClientFactory.GetClient(GraphQLClientEnum.InscriptionClient)
            .SendQueryAsync<InscriptionResultDto>(new GraphQLRequest
            {
                Query = @"query(
               $chainId:String!,$beginBlockHeight:Long!,$endBlockHeight:Long!,$skipCount:Int!,$maxResultCount:Int!
            ){
                inscription(input:{
                    chainId: $chainId
                    beginBlockHeight: $beginBlockHeight
                    endBlockHeight: $endBlockHeight
                    skipCount: $skipCount
                    maxResultCount: $maxResultCount
                }){
                  tick,
                  totalSupply,
                  issuer,
                  issueChainId,
                  blockHeight,
                  collectionExternalInfo{
                       key,
                       value
                  },
                  itemExternalInfo{
                       key,
                       value
                  },
                  owner,
                  limit,
                  deployer,
                  transactionId
                }
            }",
                Variables = new
                {
                    chainId,
                    beginBlockHeight,
                    endBlockHeight,
                    skipCount,
                    maxResultCount
                }
            });

        return graphQLResponse.Data.Inscription;
    }
    
    
    public async Task<List<SeedDto>> GetTsmSeedBySymbolsAsync(string chainId, List<string> seedSymbols)
    {
        var str = new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$seedSymbols:[String!]!){
            seedDtoList:getTsmSeedInfosBySymbol(dto: {chainId:$chainId,seedSymbols:$seedSymbols})
            {
                id,
                chainId,
				symbol,	
				seedSymbol,
       		    seedImage,
                seedName,
                blockHeight,
                status,
                registerTime,
                expireTime,
                tokenType,
                seedType,
                auctionType,
                owner,
                tokenPrice{
                    symbol,
                    amount
                },
                isBurned,
                auctionStatus,
                bidsCount,
                biddersCount,
                auctionEndTime,
                topBidPrice{
                    symbol,
                    amount
                }
            }}",
            Variables = new
            {
                chainId,
                seedSymbols
            }
        };
        var graphQlResponse = await _graphQLClient.SendQueryAsync<MySeedDto>(str);
        return graphQlResponse.Data.SeedDtoList.IsNullOrEmpty()
            ? new List<SeedDto>()
            : graphQlResponse.Data.SeedDtoList;
    }
}