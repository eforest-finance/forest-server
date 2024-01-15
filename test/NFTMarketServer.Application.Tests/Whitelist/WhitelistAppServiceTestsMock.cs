using System.Collections.Generic;
using Moq;
using NFTMarketServer.Whitelist.Dto;
using NFTMarketServer.Whitelist.Provider;

namespace NFTMarketServer.Whitelist;

public partial class WhitelistAppServiceTests
{
    private IWhitelistProvider GetMockWhitelistProvider()
    {
        var mockWhitelistProvider = new Mock<IWhitelistProvider>();

        mockWhitelistProvider.Setup(m => m.GetWhitelistByHashAsync(It.IsAny<string>(),
            It.IsAny<string>())).ReturnsAsync(
            new WhitelistInfoDto
            {
                ChainId = "tDVW",
                WhitelistHash = "test",
                ProjectId = "testProject",
                IsAvailable = true,
                IsCloneable = true,
                Remark = "test",
                Creator = "testcreator",
                StrategyType = StrategyType.Basic.ToString()
            }
        );

        mockWhitelistProvider.Setup(m => m.GetWhitelistExtraInfoListAsync(It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new ExtraInfoPageResultDto
            {
                Data = new ExtraInfoIndexList
                {
                    TotalCount = 1,
                    Items = new List<WhitelistExtraInfoDto>()
                    {
                        new WhitelistExtraInfoDto()
                        {
                            ChainId = "tDVW",
                            Address = "testAddress",
                            TagInfoId = "testProject",
                            WhitelistInfo = new WhitelistInfoBaseDto()
                            {
                                ChainId = "tDVW",
                                WhitelistHash = "test",
                                ProjectId = "testProject",
                                StrategyType = StrategyType.Basic
                            },
                            TagInfo = new TagInfoBaseDto()
                            {
                                ChainId = "tDVW",
                                TagHash = "testTagHash",
                                Name = "testTagInfo",
                                PriceTagInfo = new PriceTagInfoDto()
                                {
                                    Symbol = "ELF",
                                    Price = 1000000
                                }
                            }
                        }
                    }
                }
            }
        );
        mockWhitelistProvider.Setup(m => m.GetWhitelistManagerListAsync(It.IsAny<string>(),
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), It.IsAny<int>()))
            .ReturnsAsync(
                new WhitelistManagerResultDto
                {
                    Data = new WhitelistManagerList()
                    {
                        TotalCount = 1,
                        Items = new List<WhitelistManagerDto>()
                        {
                            new()
                            {
                                ChainId = "tDVW",
                                Manager = "testAddress",
                                WhitelistInfo = new WhitelistInfoBaseDto()
                                {
                                    ChainId = "tDVW",
                                    WhitelistHash = "test",
                                    ProjectId = "testProject",
                                    StrategyType = StrategyType.Basic
                                }
                            }
                        }
                    }
                }
            );

        mockWhitelistProvider.Setup(m => m.GetWhitelistTagInfoListAsync(It.IsAny<string>(),
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<double>(), It.IsAny<double>(),
            It.IsAny<int>(), It.IsAny<int>())).ReturnsAsync(
            new TagInfoResultDto
            {
                Data = new WhitelistTagInfoList()
                {
                    TotalCount = 1,
                    Items = new List<WhitelistTagInfoDto>()
                    {
                        new()
                        {
                            AddressCount = 1,
                            TagHash = "testWhitelistHash",
                            Name = "testName",
                            PriceTagInfo = new PriceTagInfoDto()
                            {
                                Symbol = "testSymbol",
                                Price = new decimal(1.111)
                            },
                            WhitelistInfo = new WhitelistInfoBaseDto()
                            {
                                ChainId = "tDVW",
                                WhitelistHash = "test",
                                ProjectId = "testProject",
                                StrategyType = StrategyType.Basic
                            }


                        }
                    }
                }
            }
        );
        
        return mockWhitelistProvider.Object;
    }
}