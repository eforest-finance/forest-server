using System;
using System.Collections.Generic;
using Orleans;

namespace NFTMarketServer.Users.Dto;
[GenerateSerializer]
public class UserSourceInput
{
    [Id(0)]
    public Guid UserId { get; set; }
    [Id(1)]
    public string AelfAddress { get; set; }
    [Id(2)]
    public string CaHash { get; set; }
    [Id(3)]
    public string CaAddressMain { get; set; }
    [Id(4)]
    public Dictionary<string, string> CaAddressSide { get; set; }
}