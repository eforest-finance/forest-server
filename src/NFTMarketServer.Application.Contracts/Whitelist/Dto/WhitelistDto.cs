using System.Collections.Generic;

namespace NFTMarketServer.Whitelist.Dto;

public class WhitelistInfoDto
{
    public string ChainId { get; set; }
    public string WhitelistHash { get; set; }
    public string ProjectId { get; set; }
    public bool IsAvailable { get; set; }
    public bool IsCloneable { get; set; }
    public string Remark { get; set; }
    public string Creator { get; set; }
    public string StrategyType { get; set; }
}

public class WhitelistExtraInfoDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string TagInfoId { get; set; }
    public WhitelistInfoBaseDto WhitelistInfo { get; set; }
    public TagInfoBaseDto TagInfo { get; set; }
}

public class WhitelistManagerResultDto
{
    public WhitelistManagerList Data { get; set; }
}

public class WhitelistManagerList
{
    public long TotalCount { get; set; }
    public List<WhitelistManagerDto> Items { get; set; }
}

public class WhitelistManagerDto
{
    public string ChainId { get; set; }
    public string Manager { get; set; }
    public WhitelistInfoBaseDto WhitelistInfo { get; set; }
}

public class PriceTokenDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Symbol { get; set; }
    public int Decimals { get; set; }
}

public class TagInfoResultDto
{
    public WhitelistTagInfoList Data { get; set; }
}

public class WhitelistTagInfoList
{
    public long TotalCount { get; set; }
    public List<WhitelistTagInfoDto> Items { get; set; }
}

public class WhitelistTagInfoDto : TagInfoBaseDto
{
    public WhitelistInfoBaseDto WhitelistInfo { get; set; }
    public int AddressCount { get; set; }
}
// base Dto

public class WhitelistInfoBaseDto
{
    public string ChainId { get; set; }
    public string WhitelistHash { get; set; }
    public string ProjectId { get; set; }
    public StrategyType StrategyType { get; set; }
}

public class WhitelistTagInfoBaseDto
{
    public string ChainId { get; set; }
    public string TagHash { get; set; }
    public string Name { get; set; }
    public string Info { get; set; }
    public string PriceTagInfo { get; set; }
}

public class TagInfoBaseDto
{
    public string ChainId { get; set; }
    public string TagHash { get; set; }
    public string Name { get; set; }
    public string Info { get; set; }
    public PriceTagInfoDto PriceTagInfo { get; set; }
}

public class PriceTagInfoDto
{
    public string Symbol { get; set; }
    public decimal Price { get; set; }
}

public enum StrategyType
{
    Basic,
    Price,
    Customize
}

public class ExtraInfoPageResultDto
{
    public ExtraInfoIndexList Data { get; set; }
}

public class ExtraInfoIndexList
{
    public long TotalCount { get; set; }
    public List<WhitelistExtraInfoDto> Items { get; set; }
}