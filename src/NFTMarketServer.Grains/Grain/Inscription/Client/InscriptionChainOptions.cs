namespace NFTMarketServer.Grains.Grain.Inscription.Client;

public class InscriptionChainOptions
{
    public Dictionary<string, ChainInfos> ChainInfos { get; set; }
}

public class ChainInfos
{
    public string Url { get; set; }
}