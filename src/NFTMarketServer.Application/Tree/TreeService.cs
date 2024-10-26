using System;
using System.Threading.Tasks;
using NFTMarketServer.Tree.Provider;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Tree;

public interface ITreeService
{
    Task<string> GenerateId();
    Task CreateTreeActivityAsync(CreateTreeActivicyRequest request);
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

    public async Task CreateTreeActivityAsync(CreateTreeActivicyRequest request)
    {
        await _treeActivityProvider.CreateTreeActivityAsync(request);
    }
}