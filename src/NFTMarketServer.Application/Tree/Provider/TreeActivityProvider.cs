using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Tree.Provider;

public interface ITreeActivityProvider
{
    Task CreateTreeActivityAsync(CreateTreeActivicyRequest request);
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

    public async Task CreateTreeActivityAsync(CreateTreeActivicyRequest request)
    {
        var treeActivityIndex = _objectMapper.Map<CreateTreeActivicyRequest, TreeActivityIndex>(request);

        treeActivityIndex.Id = IdGenerateHelper.ToSha256Hash(request.OriginId);
        treeActivityIndex.OriginId = request.OriginId;
        treeActivityIndex.CreateTime = DateTime.UtcNow;
        treeActivityIndex.LastModifyTime = DateTime.UtcNow;
        treeActivityIndex.TreeActivityStatus = TreeActivityStatus.NotStart;
        treeActivityIndex.HideFlag = true;
        treeActivityIndex.BeginDateTime = DateTimeHelper.FromUnixTimeMilliseconds(request.BeginDateTimeMilliseconds);
        await _treeActivityIndexRepository.AddOrUpdateAsync(treeActivityIndex);
    }
}