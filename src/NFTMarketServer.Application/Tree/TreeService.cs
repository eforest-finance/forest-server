using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Helper;
using NFTMarketServer.Tree.Provider;
using Volo.Abp.DependencyInjection;
using GuidHelper = NFTMarketServer.Common.GuidHelper;

namespace NFTMarketServer.Tree;

public interface ITreeService
{
    Task<string> GenerateIdAsync();
    Task<TreeActivityIndex> CreateTreeActivityAsync(CreateTreeActivityRequest request);
    Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request);
    Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request);
    
    Task<List<TreeActivityIndex>> GetTreeActivityListAsync(GetTreeActivityListInput request);
    
    Task<TreeActivityIndex> GetTreeActivityDetailAsync(string id);
    Task<string> CreateNewTreeActivityIdAsync();



}

public class TreeService : ITreeService, ISingletonDependency
{
    private readonly ITreeActivityProvider _treeActivityProvider;

    public TreeService(ITreeActivityProvider treeActivityProvider)
    {
        _treeActivityProvider = treeActivityProvider;
    }

    public async Task<string> GenerateIdAsync()
    {
        return Guid.NewGuid().ToString();
    }

    public async Task<TreeActivityIndex> CreateTreeActivityAsync(CreateTreeActivityRequest request)
    {
       return await _treeActivityProvider.CreateTreeActivityAsync(request);
    }

    public async Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request)
    {
        return await _treeActivityProvider.ModifyTreeActivityHideFlagAsync(request);
    }

    public async Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request)
    {
        return await _treeActivityProvider.ModifyTreeActivityStatusAsync(request);
    }

    public async Task<List<TreeActivityIndex>> GetTreeActivityListAsync(GetTreeActivityListInput request)
    {
        return await _treeActivityProvider.GetTreeActivityListAsync(request);
    }

    public async Task<TreeActivityIndex> GetTreeActivityDetailAsync(string id)
    {
        return await _treeActivityProvider.GetTreeActivityDetailAsync(id);
    }

    public async Task<string> CreateNewTreeActivityIdAsync()
    {
        return Guid.NewGuid().ToString();
    }
}