using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using MassTransit;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Grains.Grain.Tree;
using NFTMarketServer.Users.Index;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.TreeGame.Provider;

public class TreeGameUserInfoProvider : ITreeGameUserInfoProvider, ISingletonDependency
{
    private readonly INESTRepository<TreeGameUserInfoIndex, string> _treeGameUserInfoIndexRepository;
    private readonly INESTRepository<TreeGamePointsDetailInfoIndex, string> _treeGamePointsDetailIndexRepository;
    private readonly IClusterClient _clusterClient;
    private readonly ITreeGamePointsDetailProvider _treeGamePointsDetailProvider;

   // private readonly IBus _bus;
    private readonly ILogger<ITreeGameUserInfoProvider> _logger;
    private readonly IObjectMapper _objectMapper;


    public TreeGameUserInfoProvider(
        INESTRepository<TreeGameUserInfoIndex, string> treeGameUserInfoIndexRepository,
        INESTRepository<TreeGamePointsDetailInfoIndex, string> treeGamePointsDetailIndexRepository,
        //IBus bus,
        ILogger<ITreeGameUserInfoProvider> logger,
        IObjectMapper objectMapper,
        IClusterClient clusterClient,
        ITreeGamePointsDetailProvider treeGamePointsDetailProvider
    )
    {
        _treeGameUserInfoIndexRepository = treeGameUserInfoIndexRepository;
       // _bus = bus;
        _logger = logger;
        _objectMapper = objectMapper;
        _treeGamePointsDetailIndexRepository = treeGamePointsDetailIndexRepository;
        _clusterClient = clusterClient;
        _treeGamePointsDetailProvider = treeGamePointsDetailProvider;

    }
    public async Task<TreeGameUserInfoIndex> SaveOrUpdateTreeUserInfoAsync(TreeGameUserInfoDto dto)
    {
        if (dto == null)
        {
            _logger.LogError("SaveOrUpdateTreeUserBalanceAsync dto is null");
            return null;
        }
        _logger.LogInformation("SaveOrUpdateTreeUserBalanceAsync dto:{A}",JsonConvert.SerializeObject(dto));

        var treeUserIndex = _objectMapper.Map<TreeGameUserInfoDto, TreeGameUserInfoIndex>(dto);
        //update grain
        var userGrain = _clusterClient.GetGrain<ITreeUserInfoGrain>(dto.Address);
        await userGrain.SetTreeUserInfoAsync(dto);

        //update es
        await _treeGameUserInfoIndexRepository.AddOrUpdateAsync(treeUserIndex);
        return treeUserIndex;
    }

    private async Task<TreeGameUserInfoIndex> GetTreeUserInfoByEsAsync(string address)
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

    public async Task<TreeGameUserInfoIndex> GetTreeUserInfoAsync(string address)
    {
        var userGrain = _clusterClient.GetGrain<ITreeUserInfoGrain>(address);
        var treeUserInfoDto = await userGrain.GetTreeUserInfoAsync();
        var treeUserIndex = new TreeGameUserInfoIndex();
        if (treeUserInfoDto?.Data == null || treeUserInfoDto.Data.Id.IsNullOrEmpty())
        {
            treeUserIndex = await GetTreeUserInfoByEsAsync(address);
            if (treeUserIndex == null)
            {
                return null;
            }
            await userGrain.SetTreeUserInfoAsync(_objectMapper.Map<TreeGameUserInfoIndex, TreeGameUserInfoDto>(treeUserIndex));
        }else {
            treeUserIndex = _objectMapper.Map<TreeGameUserInfoDto, TreeGameUserInfoIndex>(treeUserInfoDto.Data);
        }

        return treeUserIndex;
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
    public async Task AcceptInvitationAsync(string address, string nickName, string parentAddress)
    {
        var treeUserIndex = await GetTreeUserInfoAsync(address);
        if (treeUserIndex == null)
        {
            var treeGameUserInfoDto = new TreeGameUserInfoDto()
            {
                Id = Guid.NewGuid().ToString(),
                Address = address,
                NickName = nickName,
                Points = 0,
                TreeLevel = 1,
                ParentAddress = parentAddress,
                CurrentWater = 60,
                CreateTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                WaterUpdateTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            };
            await SaveOrUpdateTreeUserInfoAsync(treeGameUserInfoDto);
        }

        var parentPointsList = await _treeGamePointsDetailProvider.GetTreePointsDetailsAsync(parentAddress);
        var invitePointsDetail = parentPointsList.FirstOrDefault(i => i.Type == PointsDetailType.INVITE);
        if (invitePointsDetail != null)
        {
            invitePointsDetail.Amount += TreeGameConstants.TreeGameInviteReward;
            await _treeGamePointsDetailProvider.SaveOrUpdateTreePointsDetailAsync(invitePointsDetail);
        }

    }
    private async Task<List<TreeGamePointsDetailInfoIndex>> GetTreePointsDetailsAsync(string address)
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