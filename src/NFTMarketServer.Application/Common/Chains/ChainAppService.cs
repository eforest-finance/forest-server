using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp;
using Volo.Abp.Modularity;

namespace NFTMarketServer.Chains
{
    [RemoteService(IsEnabled = false)]
    public class ChainAppService : NFTMarketServerAppService, IChainAppService
    {
        private readonly ServiceConfigurationContext _context;

        public ChainAppService(ServiceConfigurationContext context)
        {
            _context = context;
        }

        public async Task<string[]> GetListAsync()
        {
            var configuration = _context.Services.GetConfiguration();
            var items =configuration["Forest:Chains"];
            if (items == null)
            {
                return Array.Empty<string>();
            }

            return items.Split(",").ToArray();
        }
        
        public async Task<string> GetChainIdAsync(int index)
        {
            var listAsync = await GetListAsync();
            return index < listAsync?.Length
                ? listAsync[index]
                : throw new InvalidOperationException($"ChainId at index {index} does not exist.");
        }

        
    }
}