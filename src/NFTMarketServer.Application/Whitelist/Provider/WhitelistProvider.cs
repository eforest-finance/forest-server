using System;
using System.Threading.Tasks;
using GraphQL;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.Whitelist.Dto;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Whitelist.Provider;

public interface IWhitelistProvider
{
    Task<WhitelistInfoDto> GetWhitelistByHashAsync(string chainId, string whitelistHash);

    Task<ExtraInfoPageResultDto> GetWhitelistExtraInfoListAsync(string chainId, string projectId,
        string whitelistHash, int maxResultCount, int skipCount);

    Task<WhitelistManagerResultDto> GetWhitelistManagerListAsync(string chainId, string projectId, string whitelistHash,
        string address, int maxResultCount, int skipCount);

    Task<TagInfoResultDto> GetWhitelistTagInfoListAsync(string chainId, string projectId, string whitelistHash,
        double priceMax, double priceMin, int maxResultCount, int skipCount);
}

public class WhitelistProvider : IWhitelistProvider, ISingletonDependency
{
    private readonly ILogger<WhitelistProvider> _logger;
    private readonly IGraphQLHelper _graphQlHelper;

    public WhitelistProvider(ILogger<WhitelistProvider> logger, IGraphQLHelper graphQlHelper)
    {
        _logger = logger;
        _graphQlHelper = graphQlHelper;
    }

    public async Task<WhitelistInfoDto> GetWhitelistByHashAsync(string chainId, string whitelistHash)
    {
        try
        {
            var data = await _graphQlHelper.QueryAsync<WhitelistDto>(new GraphQLRequest
            {
                Query = @"
                  query($chainId:String,$whitelistHash:String) {
                    data:whitelistHash(input:{chainId:$chainId,whitelistHash:$whitelistHash}){
                        chainId,
                        whitelistHash,
                        projectId,
                        isAvailable,
                        isCloneable,
                        remark,
                        creator,
                        strategyType        
                    }
                }",
                Variables = new
                {
                    chainId = chainId,
                    whitelistHash = whitelistHash
                }
            });
            return data.Data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Whitelist ByHash error from GraphQL, ChainId:{chainId}, Hash:{whitelistHash}",
                chainId, whitelistHash);
            return new WhitelistInfoDto();
        }
    }

    public async Task<ExtraInfoPageResultDto> GetWhitelistExtraInfoListAsync(string chainId, string projectId,
        string whitelistHash, int maxResultCount, int skipCount)
    {
        try
        {
            var data = await _graphQlHelper.QueryAsync<ExtraInfoPageResultDto>(new GraphQLRequest
            {
                Query = @"
                    query($chainId:String!,$projectId:String!,$whitelistHash:String!,$maxResultCount:Int!,$skipCount:Int!) 
                    {
                        data:extraInfos(input:{chainId:$chainId,projectId:$projectId,whitelistHash:$whitelistHash,maxResultCount:$maxResultCount,skipCount:$skipCount})
                        {                        
                            totalCount,
                            items {
                              chainId,
                              address,
                              tagInfoId,
                              whitelistInfo {
                                chainId,
                                whitelistHash,
                                projectId,
                                strategyType
                              },
                              tagInfo {
                                chainId,
                                tagHash,
                                name,
                                info,
                                priceTagInfo {
                                  symbol,
                                  price
                                }
                              }
                            }
                        }
                    }",
                Variables = new
                {
                    chainId = chainId,
                    projectId = projectId,
                    whitelistHash = whitelistHash,
                    maxResultCount = maxResultCount,
                    skipCount = skipCount
                }
            });
            return data;
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Whitelist ExtraInfo List error from GraphQL, ChainId:{chainId}, " +
                                "ProjectId:{projectId}, Hash:{whitelistHash}", chainId, projectId, whitelistHash);
            return new ExtraInfoPageResultDto();
        }
    }

    public async Task<WhitelistManagerResultDto> GetWhitelistManagerListAsync(string chainId, string projectId,
        string whitelistHash, string address, int maxResultCount, int skipCount)
    {
        try
        {
            return await _graphQlHelper.QueryAsync<WhitelistManagerResultDto>(new GraphQLRequest
            {
                Query = @"
                    query($chainId: String!, $projectId: String!, $whitelistHash: String!, $address: String!, $maxResultCount: Int!, $skipCount: Int!) 
                    {
                        data:managerList(input:{chainId: $chainId, projectId: $projectId, whitelistHash: $whitelistHash, address: $address, maxResultCount: $maxResultCount, skipCount: $skipCount}) 
                        {
                          totalCount,
    		              items {
                            chainId,
                            manager,
                            whitelistInfo {
                              chainId,
                              whitelistHash,
                              projectId,
                              strategyType
                            }
                          }
                        }
                    }",
                Variables = new
                {
                    chainId = chainId,
                    projectId = projectId,
                    whitelistHash = whitelistHash,
                    address = address,
                    maxResultCount = maxResultCount,
                    skipCount = skipCount
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Whitelist Manager List error from GraphQL, ChainId:{chainId}, " +
                                "ProjectId:{projectId}, Hash:{whitelistHash}, Address:{address}", chainId, projectId,
                whitelistHash, address);
            return new WhitelistManagerResultDto();
        }
    }

    public async Task<TagInfoResultDto> GetWhitelistTagInfoListAsync(string chainId, string projectId,
        string whitelistHash, double priceMax, double priceMin, int maxResultCount, int skipCount)
    {
        try
        {
            return await _graphQlHelper.QueryAsync<TagInfoResultDto>(new GraphQLRequest
            {
                Query = @"
                    query($chainId: String!, $projectId: String!, $whitelistHash: String!, $priceMax: Float!, $priceMin: Float!, $maxResultCount: Int!, $skipCount: Int!) 
                    {
                        data:tagList(input:{chainId: $chainId, projectId: $projectId, whitelistHash: $whitelistHash, priceMax: $priceMax, priceMin: $priceMin, maxResultCount: $maxResultCount, skipCount: $skipCount}) 
                        {
                            totalCount,
    		                items {
                                addressCount,
                                whitelistInfo {
                                    chainId,
                                    projectId,
                                    strategyType,
                                    whitelistHash
                                },
                                tagHash,
                                chainId,
                                name,
                                info,
                                priceTagInfo {
                                  symbol,
                                  price
                                }
                            }
                         }
                      }",
                Variables = new
                {
                    chainId = chainId,
                    projectId = projectId,
                    whitelistHash = whitelistHash,
                    priceMax = priceMax,
                    priceMin = priceMin,
                    maxResultCount = maxResultCount,
                    skipCount = skipCount
                }
            });
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Query Whitelist TagInfo List error from GraphQL, ChainId:{chainId}, " +
                                "ProjectId:{projectId}, Hash:{whitelistHash}, PriceMax:{priceMax}, PriceMin:{priceMin}",
                chainId, projectId, whitelistHash, priceMax, priceMin);
            return new TagInfoResultDto();
        }
    }
    
}