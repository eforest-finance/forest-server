using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Tree.Provider;

public interface ITreeActivityProvider
{
    Task CreateTreeActivityAsync(CreateTreeActivityRequest request);
    Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request);
    Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request);
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
        var treeActivityIndex = _objectMapper.Map<CreateTreeActivityRequest, TreeActivityIndex>(request);

        treeActivityIndex.Id = IdGenerateHelper.ToSha256Hash(request.OriginId);
        treeActivityIndex.OriginId = request.OriginId;
        treeActivityIndex.CreateTime = DateTime.UtcNow;
        treeActivityIndex.LastModifyTime = DateTime.UtcNow;
        treeActivityIndex.TreeActivityStatus = TreeActivityStatus.NotStart;
        treeActivityIndex.HideFlag = true;
        treeActivityIndex.BeginDateTime = DateTimeHelper.FromUnixTimeMilliseconds(request.BeginDateTimeMilliseconds);
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
}