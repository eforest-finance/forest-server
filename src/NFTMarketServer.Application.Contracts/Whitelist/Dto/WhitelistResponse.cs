using System.Collections.Generic;

namespace NFTMarketServer.Whitelist.Dto;

public class WhitelistDto
{
    public WhitelistInfoDto Data { get; set; }
}

public class WhitelistTagInfoPriceTokenListDto
{
    public List<PriceTokenDto> Data { get; set; }
}

public class WhitelistTagInfoListDto
{
    public List<WhitelistTagInfoDto> Data { get; set; }
}

public class WhitelistManagerListDto
{
    public List<WhitelistManagerDto> Data { get; set; }
}

public class WhitelistExtraInfoList
{
    public long TotalRecordCount { get; set; }
    public List<WhitelistExtraInfoDto> Data { get; set; }
}