using System;
using System.Threading.Tasks;
using NFTMarketServer.Tree.Provider;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Tree;

public interface ITreeService
{
    Task<string> GenerateId();
    Task CreateTreeActivityAsync(CreateTreeActivityRequest request);
    Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request);
    Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request);
}

public class TreeService : ITreeService, ISingletonDependency
{
    private readonly ITreeActivityProvider _treeActivityProvider;

    public TreeService(ITreeActivityProvider treeActivityProvider)
    {
        _treeActivityProvider = treeActivityProvider;
    }

    public async Task<string> GenerateId()
    {
        return Guid.NewGuid().ToString();
    }

    public async Task CreateTreeActivityAsync(CreateTreeActivityRequest request)
    {
        await _treeActivityProvider.CreateTreeActivityAsync(request);
    }

    public async Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request)
    {
        return await _treeActivityProvider.ModifyTreeActivityHideFlagAsync(request);
    }

    public async Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request)
    {
        return await _treeActivityProvider.ModifyTreeActivityStatusAsync(request);
    }
}