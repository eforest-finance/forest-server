using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using GraphQL;
using MassTransit;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.TreeGame.Provider;

public class TreeGamePointsRecordProvider : ITreeGamePointsRecordProvider, ISingletonDependency
{
    private readonly INESTRepository<TreeGamePointsDetailInfoIndex, string> _treeGamePointsDetailIndexRepository;
    private readonly IBus _bus;
    private readonly ILogger<ITreeGamePointsDetailProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IGraphQLHelper _graphQlHelper;


    public TreeGamePointsRecordProvider(
        IGraphQLHelper graphQlHelper,
        INESTRepository<TreeGamePointsDetailInfoIndex, string> treeGamePointsDetailIndexRepository,
        IBus bus,
        ILogger<ITreeGamePointsDetailProvider> logger,
        IObjectMapper objectMapper
    )
    {
        _treeGamePointsDetailIndexRepository = treeGamePointsDetailIndexRepository;
        _bus = bus;
        _logger = logger;
        _objectMapper = objectMapper;
        _graphQlHelper = graphQlHelper;

    }

    public async Task<IndexerTreePointsRecordPage> GetSyncTreePointsRecordsAsync(long startBlockHeight, long endBlockHeight,string chainId)
    {
        var graphQLResponse = await _graphQlHelper.QueryAsync<IndexerTreePointsRecordPage>(new GraphQLRequest
        {
            Query = @"
			    query($startBlockHeight:Long!,$endBlockHeight:Long!,$chainId:String!) {
                    data:getSyncTreePointsRecords(dto:{startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,chainId:$chainId}){
                        totalRecordCount,
                        treePointsChangeRecordList:data{
                             id,
                             address,
                             totalPoints,
                             points,
                             opType,
                             opTime,
                             pointsType,
                             activityId,
                             treeLevel,
                             chainId,
                             blockHeight                                   
                         }
                    }
                }",
            Variables = new
            {
                startBlockHeight = startBlockHeight,
                endBlockHeight = endBlockHeight,
                chainId = chainId
            }
        });
        return graphQLResponse.Data.Data;
    }
}