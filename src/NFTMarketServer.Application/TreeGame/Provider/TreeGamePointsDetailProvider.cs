using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using NFTMarketServer.Grains.Grain.Tree;
using NFTMarketServer.Users.Index;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.TreeGame.Provider;

public class TreeGamePointsDetailProvider : ITreeGamePointsDetailProvider, ISingletonDependency
{
    private readonly INESTRepository<TreeGamePointsDetailInfoIndex, string> _treeGamePointsDetailIndexRepository;
    private readonly IBus _bus;
    private readonly ILogger<ITreeGamePointsDetailProvider> _logger;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;


    public TreeGamePointsDetailProvider(
        INESTRepository<TreeGamePointsDetailInfoIndex, string> treeGamePointsDetailIndexRepository,
        IBus bus,
        ILogger<ITreeGamePointsDetailProvider> logger,
        IObjectMapper objectMapper,
        IClusterClient clusterClient
    )
    {
        _treeGamePointsDetailIndexRepository = treeGamePointsDetailIndexRepository;
        _bus = bus;
        _logger = logger;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;

    }

    public async Task BulkSaveOrUpdateTreePointsDetailsAsync(string address, List<TreeGamePointsDetailInfoIndex> pointsDetailList)
    {
        var pointsGrain = _clusterClient.GetGrain<ITreeUserPointsDetailGrain>(address);
        var pointsDetailDtoList = pointsDetailList.Select(x => _objectMapper.Map<TreeGamePointsDetailInfoIndex, TreeGamePointsDetailInfoDto>(x)).ToList();
        
        await pointsGrain.SetTreeUserPointsDetailListAsync(pointsDetailDtoList);        
        
        foreach (var points in pointsDetailList)
        {
            await _treeGamePointsDetailIndexRepository.AddOrUpdateAsync(points);
        }
    }

    public async Task<TreeGamePointsDetailInfoIndex> SaveOrUpdateTreePointsDetailAsync(TreeGamePointsDetailInfoIndex info)
    {
        var pointsGrain = _clusterClient.GetGrain<ITreeUserPointsDetailGrain>(info.Address);
        var pointsDetailState = await pointsGrain.GetTreeUserPointsDetailListAsync();
        var pointsDetails = pointsDetailState.Data;
        foreach (var detail in pointsDetails)
        {
            if (detail.Type == info.Type)
            {
                detail.Amount = info.Amount;
                detail.RemainingTime = info.RemainingTime;
                detail.UpdateTime = info.UpdateTime;
            }
        }

        await pointsGrain.SetTreeUserPointsDetailListAsync(pointsDetails);        
        await _treeGamePointsDetailIndexRepository.AddOrUpdateAsync(info);
        return info;
    }

    public async Task<List<TreeGamePointsDetailInfoIndex>> GetTreePointsDetailsByEsAsync(string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TreeGamePointsDetailInfoIndex>, QueryContainer>>();
        if (!address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(address)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TreeGamePointsDetailInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<TreeGamePointsDetailInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Ascending(t => t.Type));
        var tuple = await _treeGamePointsDetailIndexRepository.GetSortListAsync(Filter, sortFunc: sorting);
        if (tuple == null || tuple.Item1 == 0)
        {
            return null;
        }

        return tuple.Item2;
    }
    public async Task<List<TreeGamePointsDetailInfoIndex>> GetTreePointsDetailsAsync(string address)
    {
        var pointsGrain = _clusterClient.GetGrain<ITreeUserPointsDetailGrain>(address);
        var pointsDetails = await pointsGrain.GetTreeUserPointsDetailListAsync();
        var pointsDetailIndexList = new List<TreeGamePointsDetailInfoIndex>();
        if (pointsDetails == null || pointsDetails.Data.IsNullOrEmpty())
        {
            pointsDetailIndexList = await GetTreePointsDetailsByEsAsync(address);
            if (pointsDetailIndexList.IsNullOrEmpty())
            {
                return null;
            }

            var pointsDetailDtoList = pointsDetailIndexList.Select(x => _objectMapper.Map<TreeGamePointsDetailInfoIndex, TreeGamePointsDetailInfoDto>(x)).ToList();
            await pointsGrain.SetTreeUserPointsDetailListAsync(pointsDetailDtoList);
        }
        else
        {
            pointsDetailIndexList = pointsDetails.Data
                .Select(x => _objectMapper.Map<TreeGamePointsDetailInfoDto, TreeGamePointsDetailInfoIndex>(x)).ToList();
        }

        return pointsDetailIndexList;
        
    }
}