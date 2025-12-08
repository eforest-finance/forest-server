using System.Threading.Tasks;
using NFTMarketServer.Whitelist.Dto;

namespace NFTMarketServer.Whitelist;

public interface IWhitelistAppService
{
    Task<WhitelistInfoDto> GetWhitelistByHashAsync(GetWhitelistByHashDto input);
    Task<ExtraInfoIndexList> GetWhitelistExtraInfoListAsync(GetWhitelistExtraInfoListDto input);
    Task<WhitelistManagerList> GetWhitelistManagerListAsync(GetWhitelistManagerListDto input);
    Task<WhitelistTagInfoList> GetWhitelistTagInfoListAsync(GetTagInfoListDto input);
}