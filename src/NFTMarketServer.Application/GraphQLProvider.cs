using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Basic;
using NFTMarketServer.Bid.Dtos;
using NFTMarketServer.Chain;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.ApplicationHandler;
using NFTMarketServer.Inscription;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Provider;
using NFTMarketServer.Seed.Dto;
using NFTMarketServer.Seed.Index;
using Orleans;
using Volo.Abp.DependencyInjection;
using TokenType = NFTMarketServer.Seed.Dto.TokenType;

namespace NFTMarketServer;

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly GraphQLHttpClient _graphQLClient;
    private readonly GraphQLOptions _graphQLOptions;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly IGraphQLClientFactory _graphQlClientFactory;


    public GraphQLProvider(ILogger<GraphQLProvider> logger, IClusterClient clusterClient,
        IOptionsSnapshot<GraphQLOptions> graphQLOptions, IGraphQLClientFactory graphQlClientFactory)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _graphQLOptions = graphQLOptions.Value;
        _graphQLClient = new GraphQLHttpClient(_graphQLOptions.Configuration, new NewtonsoftJsonSerializer());
        _graphQlClientFactory = graphQlClientFactory;
    }


    public async Task<long> GetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() + chainId);
            return await grain.GetStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeight on chain {id} error", chainId);
            return CommonConstant.LongError;
        }
    }

    public async Task SetLastEndHeightAsync(string chainId, BusinessQueryChainType queryChainType, long height)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(queryChainType.ToString() +
                                                                              chainId);
            await grain.SetStateAsync(height);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetIndexBlockHeight on chain {id} error", chainId);
        }
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        var graphQLResponse = await _graphQLClient.SendQueryAsync<ConfirmedBlockHeightRecord>(new GraphQLRequest
        {
            Query = 
                @"query($chainId:String,$filterType:BlockFilterType!) {
                    syncState(dto: {chainId:$chainId,filterType:$filterType}){
                        confirmedBlockHeight}
                    }",
            Variables = new
            {
                chainId,
                filterType = BlockFilterType.LOG_EVENT
            }
        });

        return graphQLResponse.Data.SyncState.ConfirmedBlockHeight;
    }

    public async Task<List<AuctionInfoDto>> GetSyncSymbolAuctionRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<SymbolAuctionRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
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
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
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
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
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
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
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
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
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

    public async Task<List<SeedSymbolIndex>> GetSyncSeedSymbolRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<IndexerSeedSymbolSync>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
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

    public async Task<List<BidInfoDto>> GetSyncSymbolBidRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<SymbolBidRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
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
}