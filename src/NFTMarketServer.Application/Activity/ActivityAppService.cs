using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.Activity.Index;
using NFTMarketServer.Activity.Provider;
using NFTMarketServer.Users.Provider;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Activity;

public class ActivityAppService : IActivityAppService, ISingletonDependency
{
    private readonly IActivityProvider _activityProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IObjectMapper _objectMapper;

    public ActivityAppService(IActivityProvider activityProvider,
        IUserInformationProvider userInformationProvider,
        IObjectMapper objectMapper)
    {
        _activityProvider = activityProvider;
        _userInformationProvider = userInformationProvider;
        _objectMapper = objectMapper;
    }

    public async Task<PagedResultDto<SymbolMarketActivityDto>> GetListAsync(GetActivitiesInput input)
    {
        var allAddress = await _userInformationProvider.GetFullAddressAsync(input.Address);
        var result = await _activityProvider.GetActivityListAsync(allAddress,input.Types,input.SkipCount,input.MaxResultCount
        );
        if (result == null)
        {
            return new PagedResultDto<SymbolMarketActivityDto>
            {
                Items = new List<SymbolMarketActivityDto>(),
                TotalCount = 0
            };
        }
        var items = result.IndexerActivityList.Select(Map).ToList();
        
        var totalCount = result.TotalRecordCount;
        
        return new PagedResultDto<SymbolMarketActivityDto>
        {
            Items = items,
            TotalCount = totalCount
        };
    }
    
    private SymbolMarketActivityDto Map(IndexerActivity index)
    {
        if (index == null) return null;
        var symbolMarketActivityDto = _objectMapper.Map<IndexerActivity, SymbolMarketActivityDto>(index);
        return symbolMarketActivityDto;
    }
}