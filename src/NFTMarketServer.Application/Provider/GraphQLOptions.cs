using System.Collections.Generic;

namespace NFTMarketServer.Provider;

public class GraphQLOptions
{
    public string Configuration { get; set; }
    public string InscriptionConfiguration { get; set; }
    
    public string DropConfiguration { get; set; }
    
    public string SchrodingerConfiguration { get; set; }
    public string BasicConfiguration { get; set; }
    public string InscriptionBasicConfiguration { get; set; }

}

public class ForestChainOptions
{
    public List<string> Chains { get; set; }
}