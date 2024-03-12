using Nest;
using Newtonsoft.Json;

namespace NFTMarketServer.Entities;

public class ExternalInfoDictionary
{
    [JsonProperty(PropertyName = "key")]
    [Keyword] public string Key { get; set; }
    [JsonProperty(PropertyName = "value")]
    [Keyword] public string Value { get; set; }
}

public class AttributeDictionary
{
    [JsonProperty(PropertyName = "traitType")]
    [Keyword] public string TraitType { get; set; }
    [JsonProperty(PropertyName = "value")]
    [Keyword] public string Value { get; set; }
}

public class InscriptionDeploy
{
    [JsonProperty(PropertyName = "lim")]
    [Keyword] public string Lim { get; set; }
}

public class InscriptionAdop
{
    [JsonProperty(PropertyName = "gen")]
    [Keyword] public string Gen { get; set; }
}