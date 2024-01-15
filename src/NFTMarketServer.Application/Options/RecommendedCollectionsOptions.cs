using System.Collections.Generic;

namespace NFTMarketServer.Options;

public class RecommendedCollectionsOptions
{
    public List<RecommendedCollection> RecommendedCollections { get; set; }
}

public class RecommendedCollection
{ 
    public string id { get; set; }
}