using System;
using System.Collections.Generic;

namespace NFTMarketServer.Users.Dto;

public class UserSourceInput
{
    public Guid UserId { get; set; }
    public string AelfAddress { get; set; }
    public string CaHash { get; set; }
    public string CaAddressMain { get; set; }
    public Dictionary<string, string> CaAddressSide { get; set; }
}