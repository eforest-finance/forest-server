using System;
using System.Collections.Generic;
using Orleans;

namespace NFTMarketServer.Users.Dto;
[GenerateSerializer]
public class CreatePlatformNFTGrainInput
{
    [Id(0)]
    public string Address { get; set; }
    [Id(1)]
    public bool IsBack { get; set; }
}