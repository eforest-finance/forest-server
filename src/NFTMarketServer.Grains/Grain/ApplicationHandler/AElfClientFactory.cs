using System.Collections.Concurrent;
using AElf.Client.Service;
using Microsoft.Extensions.Options;

namespace NFTMarketServer.Grains.Grain.ApplicationHandler
{
    public class AElfClientFactory : IBlockchainClientFactory<AElfClient>
    {
        private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;
        private readonly ConcurrentDictionary<string, AElfClient> _clientDic;

        public AElfClientFactory(IOptionsMonitor<ChainOptions> chainOptionsMonitor)
        {
            _chainOptionsMonitor = chainOptionsMonitor;
            _clientDic = new ConcurrentDictionary<string, AElfClient>();
        }

        public AElfClient GetClient(string chainName)
        {
            var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainName];
            if (_clientDic.TryGetValue(chainName, out var client))
            {
                return client;
            }

            client = new AElfClient(chainInfo.BaseUrl);
            _clientDic[chainName] = client;
            return client;
        }
    }
}