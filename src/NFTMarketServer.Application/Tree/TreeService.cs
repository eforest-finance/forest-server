using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.Grains.Grain.Tree;
using NFTMarketServer.Helper;
using NFTMarketServer.Tree.Provider;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using GuidHelper = NFTMarketServer.Common.GuidHelper;

namespace NFTMarketServer.Tree;

public interface ITreeService
{
    Task<string> GenerateIdAsync();
    Task<TreeActivityIndex> CreateTreeActivityAsync(CreateTreeActivityRequest request);
    Task<bool> ModifyTreeActivityHideFlagAsync(ModifyTreeActivityHideFlagRequest request);
    Task<bool> ModifyTreeActivityStatusAsync(ModifyTreeActivityStatusRequest request);
    
    Task<List<TreeActivityDto>> GetTreeActivityListAsync(GetTreeActivityListInput request);
    
    Task<TreeActivityDto> GetTreeActivityDetailAsync(string id, string address);
    Task<string> CreateNewTreeActivityIdAsync();



}

public class TreeService : ITreeService, ISingletonDependency
{
    private readonly ITreeActivityProvider _treeActivityProvider;
    private readonly IObjectMapper _objectMapper;
    private readonly IClusterClient _clusterClient;

    public TreeService(ITreeActivityProvider treeActivityProvider,
        IClusterClient clusterClient,
        IObjectMapper objectMapper
        )
    {
        _treeActivityProvider = treeActivityProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;

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

    public async Task<List<TreeActivityDto>> GetTreeActivityListAsync(GetTreeActivityListInput request)
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
        var sortActivityDtos = sortActivityList.Select(i => _objectMapper.Map<TreeActivityIndex,TreeActivityDto>(i)).ToList();
        foreach (var record in sortActivityDtos)
        {
            if (request.Address.IsNullOrEmpty())
            {
                break;
            }

            if (record.LeftReward < 0)
            {
                record.LeftReward = 0;
            }

            var activityRecordGrain = _clusterClient.GetGrain<ITreeUserActivityRecordGrain>(string.Concat(request.Address,"_",record.Id));
            var activityRecord = await activityRecordGrain.GetTreeUserActivityRecordAsync();
            if (activityRecord == null || activityRecord.Data == null || activityRecord.Data.ClaimCount <= 0)
            {
                record.CanClaim = true;
            }
            else
            {
                record.CanClaim = false;
            }
        }
        
        return sortActivityDtos;
    }

    public async Task<TreeActivityDto> GetTreeActivityDetailAsync(string id, string address)
    {
        var sortActivityIndex = await _treeActivityProvider.GetTreeActivityDetailAsync(id);
        var sortActivityDto = _objectMapper.Map<TreeActivityIndex, TreeActivityDto>(sortActivityIndex);
        if (address.IsNullOrEmpty())
        {
            return sortActivityDto;
        }
        var activityRecordGrain = _clusterClient.GetGrain<ITreeUserActivityRecordGrain>(string.Concat(address,"_",id));
        var activityRecord = await activityRecordGrain.GetTreeUserActivityRecordAsync();
        if (activityRecord == null || activityRecord.Data == null || activityRecord.Data.ClaimCount <= 0)
        {
            sortActivityDto.CanClaim = true;
        }
        else
        {
            sortActivityDto.CanClaim = false;
        }
        if (sortActivityDto.LeftReward < 0)
        {
            sortActivityDto.LeftReward = 0;
        }
        return sortActivityDto;
    }

    public async Task<string> CreateNewTreeActivityIdAsync()
    {
        return Guid.NewGuid().ToString();
    }
}