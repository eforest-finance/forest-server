using System;
using System.Collections.Generic;

namespace NFTMarketServer.Users.Dto;

public class CreatePlatformNFTGrainInput
{
    public string Address { get; set; }
    public bool IsBack { get; set; }
}