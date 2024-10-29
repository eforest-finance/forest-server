using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Users.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Tree.Provider;

public interface ITreeActivityProvider
{
    Task CreateTreeActivityAsync(CreateTreeActivityRequest request);
    Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request);
    Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request);
    Task<List<TreeActivityIndex>> GetTreeActivityListAsync(GetTreeActivityListInput request);
}

public class TreeActivityProvider : ITreeActivityProvider, ISingletonDependency
{
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<TreeActivityIndex, string> _treeActivityIndexRepository;

    public TreeActivityProvider(IObjectMapper objectMapper,
    INESTRepository<TreeActivityIndex, string> treeActivityIndexRepository)
    {
        _objectMapper = objectMapper;
        _treeActivityIndexRepository = treeActivityIndexRepository;
    }

    public async Task CreateTreeActivityAsync(CreateTreeActivityRequest request)
    {
        if (request.MinPoints < 0 && request.CostPoints < 0)
        {
            throw new Exception("Invalid request points");
        }

        var treeActivityIndex = _objectMapper.Map<CreateTreeActivityRequest, TreeActivityIndex>(request);

        treeActivityIndex.Id = IdGenerateHelper.ToSha256Hash(request.OriginId);
        treeActivityIndex.OriginId = request.OriginId;
        treeActivityIndex.CreateTime = DateTime.UtcNow;
        treeActivityIndex.LastModifyTime = DateTime.UtcNow;
        treeActivityIndex.TreeActivityStatus = TreeActivityStatus.NotStart;
        treeActivityIndex.HideFlag = true;
        treeActivityIndex.BeginDateTime = DateTimeHelper.FromUnixTimeMilliseconds(request.BeginDateTimeMilliseconds);
        if (request.RedeemType == RedeemType.Free)
        {
            treeActivityIndex.CostPoints = 0;
        }

        await _treeActivityIndexRepository.AddOrUpdateAsync(treeActivityIndex);
    }

    public async Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request)
    {
        var treeActivityIndex = await _treeActivityIndexRepository.GetAsync(request.Id);
        if (treeActivityIndex == null) return false;
        if (treeActivityIndex.HideFlag == request.HideFlag) return true;
        treeActivityIndex.HideFlag = request.HideFlag;
        treeActivityIndex.LastModifyTime = DateTime.UtcNow;
        await _treeActivityIndexRepository.AddOrUpdateAsync(treeActivityIndex);
        return true;
    }

    public async Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request)
    {
        var treeActivityIndex = await _treeActivityIndexRepository.GetAsync(request.Id);
        if (treeActivityIndex == null) return false;
        if (treeActivityIndex.TreeActivityStatus == request.TreeActivityStatus) return true;
        treeActivityIndex.TreeActivityStatus = request.TreeActivityStatus;
        treeActivityIndex.LastModifyTime = DateTime.UtcNow;
        await _treeActivityIndexRepository.AddOrUpdateAsync(treeActivityIndex);
        return true;
    }

    public async Task<List<TreeActivityIndex>> GetTreeActivityListAsync(GetTreeActivityListInput request)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<TreeActivityIndex>, QueryContainer>>();
        QueryContainer Filter(QueryContainerDescriptor<TreeActivityIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<TreeActivityIndex>, IPromise<IList<ISort>>>(s =>
            s.Ascending(t => t.TreeActivityStatus));
        var tuple = await _treeActivityIndexRepository.GetSortListAsync(Filter, sortFunc: sorting);
        if (tuple == null || tuple.Item1 == 0)
        {
            return null;
        }
        return tuple.Item2;
    }
}