using System;
using System.Collections.Generic;
using System.Linq;
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
        var activityList = await _treeActivityProvider.GetTreeActivityListAsync(request);
        var sortActivityList = new List<TreeActivityIndex>();
        var ongoingList = activityList
            .Where(i => i.TreeActivityStatus == TreeActivityStatus.Active && i.LeftReward > 0 &&
                        i.BeginDateTime < DateTime.UtcNow).OrderBy(i=>i.BeginDateTime).ToList();
        var toStartList = activityList
            .Where(i => i.TreeActivityStatus == TreeActivityStatus.Active && i.LeftReward > 0 &&
                        i.BeginDateTime >= DateTime.UtcNow).OrderBy(i=>i.BeginDateTime).ToList();
        var notStartList = activityList
            .Where(i => i.TreeActivityStatus == TreeActivityStatus.NotStart && i.LeftReward > 0).OrderBy(i=>i.BeginDateTime).ToList();
        var endList = activityList
            .Where(i => i.LeftReward <= 0).OrderBy(i=>i.BeginDateTime).ToList();
        sortActivityList.AddRange(ongoingList);
        sortActivityList.AddRange(toStartList);
        sortActivityList.AddRange(notStartList);
        sortActivityList.AddRange(endList);
        return sortActivityList;
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