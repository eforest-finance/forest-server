using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.TreeGame.Provider;

public class TreeGamePointsDetailProvider : ITreeGamePointsDetailProvider, ISingletonDependency
{
    private readonly INESTRepository<TreeGamePointsDetailInfoIndex, string> _treeGamePointsDetailIndexRepository;
    private readonly IBus _bus;
    private readonly ILogger<ITreeGamePointsDetailProvider> _logger;
    private readonly IObjectMapper _objectMapper;


    public TreeGamePointsDetailProvider(
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

    }

    public async Task BulkSaveOrUpdateTreePointsDetaislAsync(List<TreeGamePointsDetailInfoIndex> treeGameUserInfos)
    {
        await _treeGamePointsDetailIndexRepository.BulkAddOrUpdateAsync(treeGameUserInfos);
    }

    public async Task<TreeGamePointsDetailInfoIndex> SaveOrUpdateTreePointsDetailAsync(TreeGamePointsDetailInfoIndex info)
    {
        await _treeGamePointsDetailIndexRepository.AddOrUpdateAsync(info);
        return info;
    }

    public async Task<List<TreeGamePointsDetailInfoIndex>> GetTreePointsDetailsAsync(string address)
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
}