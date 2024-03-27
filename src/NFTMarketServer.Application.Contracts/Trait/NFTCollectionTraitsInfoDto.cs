#nullable enable
using System.Collections.Generic;

namespace NFTMarketServer.Trait;

public class NFTCollectionTraitInfoDto
{
    public string Key { get; set; }
    public int ValueCount { get; set; }

    public List<ValueCountDictionary> values { get; set; }
}

public class ValueCountDictionary
{
    public string Value { get; set; }
    public long ItemsCount { get; set; }
}