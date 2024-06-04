using System.Collections.Generic;

namespace NFTMarketServer.Options;

public class ChainOption
{
    public Dictionary<string, string> NodeApis { get; set; } = new();
    public Dictionary<string, string> AccountPrivateKey { get; set; } = new();
    public Dictionary<string, Dictionary<string, string>> ContractAddress { get; set; } = new();
}