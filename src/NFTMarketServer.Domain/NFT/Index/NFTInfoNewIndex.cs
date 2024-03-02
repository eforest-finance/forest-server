using System;
using System.Collections.Generic;
using Nest;
using NFTMarketServer.Entities;

namespace NFTMarketServer.NFT.Index;

public class NFTInfoNewIndex : NFTInfoIndex
{
    public int Generation { get; set; } = 0;
    [Nested]
    public List<ExternalInfoDictionary> TraitPairsDictionary { get; set; }
}