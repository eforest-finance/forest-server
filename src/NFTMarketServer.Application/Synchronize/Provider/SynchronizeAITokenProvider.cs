using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Synchronize.Provider;

public interface ISynchronizeAITokenProvider
{
    public Task SaveSynchronizeJobAsync(SynchronizeAITokenJobInfoIndex syncIndex);
    public Task<SynchronizeAITokenJobInfoIndex> GetSynchronizeJobBySymbolAsync(string symbol);
    public Task<List<SynchronizeAITokenJobInfoIndex>> GetSynchronizeJobByStatusAsync(List<string> status);

}

public class SynchronizeAITokenProvider : ISynchronizeAITokenProvider, ISingletonDependency
{
    private readonly INESTRepository<SynchronizeAITokenJobInfoIndex, string> _synchronizeAITokenJobInfoIndexRepository;
    private readonly IObjectMapper _objectMapper;

    public SynchronizeAITokenProvider(
        INESTRepository<SynchronizeAITokenJobInfoIndex, string> synchronizeAITokenJobInfoIndexRepository,
        IObjectMapper objectMapper)
    {
        _synchronizeAITokenJobInfoIndexRepository = synchronizeAITokenJobInfoIndexRepository;
        _objectMapper = objectMapper;
    }


    public async Task SaveSynchronizeJobAsync(SynchronizeAITokenJobInfoIndex syncIndex)
    {
        await _synchronizeAITokenJobInfoIndexRepository.AddOrUpdateAsync(syncIndex);
    }

    public async Task<SynchronizeAITokenJobInfoIndex> GetSynchronizeJobBySymbolAsync(string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SynchronizeAITokenJobInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Symbol).Terms(symbol)));

        QueryContainer Filter(QueryContainerDescriptor<SynchronizeAITokenJobInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, syncTxs) = await _synchronizeAITokenJobInfoIndexRepository.GetListAsync(Filter);

        return totalCount < 1
            ? null
            : syncTxs.FirstOrDefault();
    }

    public Task<List<SynchronizeAITokenJobInfoIndex>> GetSynchronizeJobByStatusAsync(List<string> status)
    {
        if (status.IsNullOrEmpty())
        {
            return null;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<SynchronizeAITokenJobInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Symbol).Terms(status)));

        QueryContainer Filter(QueryContainerDescriptor<SynchronizeAITokenJobInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        return await _synchronizeAITokenJobInfoIndexRepository.GetListAsync(Filter);
    }
}