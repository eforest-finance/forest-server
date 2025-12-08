using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.Whitelist.Dto;
using NFTMarketServer.Whitelist.Provider;

namespace NFTMarketServer.Whitelist;

public class WhitelistAppService : NFTMarketServerAppService, IWhitelistAppService
{
    private readonly IWhitelistProvider _whitelistProvider;

    public WhitelistAppService(IWhitelistProvider whitelistProvider)
    {
        _whitelistProvider = whitelistProvider;
    }

    public async Task<WhitelistInfoDto> GetWhitelistByHashAsync(GetWhitelistByHashDto input)
    {
        return await _whitelistProvider.GetWhitelistByHashAsync(input.ChainId, input.WhitelistHash);
    }

    public async Task<ExtraInfoIndexList> GetWhitelistExtraInfoListAsync(GetWhitelistExtraInfoListDto input)
    {
        var result = await _whitelistProvider.GetWhitelistExtraInfoListAsync(input.ChainId, input.ProjectId,
            input.WhitelistHash, input.MaxResultCount, input.SkipCount);
        var data = result.Data;

        // Filter by input CurrentAddress to show extra information.
        var filterResult = data.Items.Where(item => item.Address == input.CurrentAddress).ToList();

        // Manager also has permission to view all information.
        var whitelistManager = await _whitelistProvider.GetWhitelistManagerListAsync(input.ChainId, input.ProjectId,
            input.WhitelistHash, input.CurrentAddress, 100, 0);
        if (whitelistManager.Data.Items.Any(e => e.Manager == input.CurrentAddress))
        {
            filterResult = data.Items;
        }

        // Search by input SearchAddress to show extra information.
        if (!string.IsNullOrEmpty(input.SearchAddress))
        {
            filterResult = filterResult.Where(item => item.Address.Contains(input.SearchAddress)).ToList();
        }

        if (input.TagHash != "ALL")
        {
            filterResult = filterResult.Where(item => item.TagInfo.TagHash == input.TagHash).ToList();
        }

        return new ExtraInfoIndexList()
        {
            Items = filterResult,
            TotalCount = data.TotalCount
        };
    }

    public async Task<WhitelistManagerList> GetWhitelistManagerListAsync(GetWhitelistManagerListDto input)
    {
        var result = await _whitelistProvider.GetWhitelistManagerListAsync(input.ChainId, input.ProjectId,
            input.WhitelistHash, input.Address, input.MaxResultCount, input.SkipCount);
        return result.Data;
    }

    public async Task<WhitelistTagInfoList> GetWhitelistTagInfoListAsync(GetTagInfoListDto input)
    {
        var result = await _whitelistProvider.GetWhitelistTagInfoListAsync(input.ChainId, input.ProjectId,
            input.WhitelistHash, input.PriceMax, input.PriceMin, input.MaxResultCount, input.SkipCount);
        return result.Data;
    }
}