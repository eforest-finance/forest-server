using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Message.Provider;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Users.Provider;

public class TreeGameUserInfoProvider : ITreeGameUserInfoProvider, ISingletonDependency
{
    private readonly INESTRepository<TreeGameUserInfoIndex, string> _treeGameUserInfoIndexRepository;
    private readonly IBus _bus;
    private readonly ILogger<MessageInfoProvider> _logger;
    private readonly IObjectMapper _objectMapper;


    public TreeGameUserInfoProvider(
        INESTRepository<TreeGameUserInfoIndex, string> treeGameUserInfoIndexRepository,
        IBus bus,
        ILogger<MessageInfoProvider> logger,
        IObjectMapper objectMapper
    )
    {
        _treeGameUserInfoIndexRepository = treeGameUserInfoIndexRepository;
        _bus = bus;
        _logger = logger;
        _objectMapper = objectMapper;

    }
    public async Task<TreeGameUserInfoIndex> SaveOrUpdateTreeUserBalanceAsync(TreeGameUserInfoDto dto)
    {
        if (dto == null)
        {
            _logger.LogError("SaveOrUpdateTreeUserBalanceAsync dto is null");
            return null;
        }
        _logger.LogInformation("SaveOrUpdateTreeUserBalanceAsync dto:{A}",JsonConvert.SerializeObject(dto));

        var treeUserIndex = _objectMapper.Map<TreeGameUserInfoDto, TreeGameUserInfoIndex>(dto);
        treeUserIndex.Id = Guid.NewGuid().ToString();
        treeUserIndex.CreateTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow);
        await _treeGameUserInfoIndexRepository.AddOrUpdateAsync(treeUserIndex);
        return treeUserIndex;
    }

    public async Task<TreeGameUserInfoIndex> GetTreeUserInfoAsync(string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TreeGameUserInfoIndex>, QueryContainer>>();
        if (!address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(address)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TreeGameUserInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<TreeGameUserInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.CreateTime));
        var tuple = await _treeGameUserInfoIndexRepository.GetSortListAsync(Filter, sortFunc: sorting);
        if (tuple == null || tuple.Item1 == 0)
        {
            return null;
        }
        return tuple.Item2.FirstOrDefault();
    }

    public async Task<Tuple<long, List<TreeGameUserInfoIndex>>> GetTreeUsersByParentUserAsync(string address)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TreeGameUserInfoIndex>, QueryContainer>>();
        if (!address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.ParentAddress).Value(address)));
        }
        QueryContainer Filter(QueryContainerDescriptor<TreeGameUserInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<TreeGameUserInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.CreateTime));
        var tuple = await _treeGameUserInfoIndexRepository.GetSortListAsync(Filter, sortFunc: sorting);
        if (tuple == null || tuple.Item1 == 0)
        {
            return null;
        }
        return tuple;
    }
}