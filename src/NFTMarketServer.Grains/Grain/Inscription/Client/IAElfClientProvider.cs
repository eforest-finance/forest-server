using AElf.Client.Service;

namespace NFTMarketServer.Grains.Grain.Inscription.Client;

public interface IAElfClientProvider
{
    AElfClient GetClient(string chainName);
}