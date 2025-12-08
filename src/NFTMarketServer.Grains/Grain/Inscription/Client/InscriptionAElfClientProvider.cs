using System.Collections.Concurrent;
using AElf.Client.Service;
using Microsoft.Extensions.Options;

namespace NFTMarketServer.Grains.Grain.Inscription.Client;

public class InscriptionAElfClientProvider : IAElfClientProvider
{
    private readonly IOptionsMonitor<InscriptionChainOptions> _chainOptionsMonitor;
    private readonly ConcurrentDictionary<string, Lazy<AElfClient>> _clientDic;

    public InscriptionAElfClientProvider(IOptionsMonitor<InscriptionChainOptions> chainOptionsMonitor)
    {
        _chainOptionsMonitor = chainOptionsMonitor;
        _clientDic = new ConcurrentDictionary<string, Lazy<AElfClient>>();
    }


    public AElfClient GetClient(string chainName)
    {
        var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainName];
        var client = _clientDic.GetOrAdd(chainName, _ => new Lazy<AElfClient>(() =>
        {
            var client = new AElfClient(chainInfo.Url);
            return client;
        })).Value;
        return client;
    }
}