using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Synchronize.Dto;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Synchronize.Provider;

public interface ISynchronizeTransactionProvider
{
    public Task<SynchronizeTransactionDto> GetSynchronizeJobByTxHashAsync(Guid? userId, string txHash);
    public Task<SynchronizeTransactionDto> GetSynchronizeJobBySymbolAsync(string symbol);
}

public class SynchronizeTransactionProvider : ISynchronizeTransactionProvider, ISingletonDependency
{
    private readonly INESTRepository<SynchronizeTransactionInfoIndex, string> _synchronizeTransactionRepository;
    private readonly IObjectMapper _objectMapper;

    public SynchronizeTransactionProvider(
        INESTRepository<SynchronizeTransactionInfoIndex, string> synchronizeTransactionRepository,
        IObjectMapper objectMapper)
    {
        _synchronizeTransactionRepository = synchronizeTransactionRepository;
        _objectMapper = objectMapper;
    }


    public async Task<SynchronizeTransactionDto> GetSynchronizeJobByTxHashAsync(Guid? userId, string txHash)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SynchronizeTransactionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.TxHash).Terms(txHash)));
        if (userId != null)
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.UserId).Terms(userId)));
        }

        QueryContainer Filter(QueryContainerDescriptor<SynchronizeTransactionInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, syncTxs) = await _synchronizeTransactionRepository.GetListAsync(Filter);

        return totalCount < 1
            ? new SynchronizeTransactionDto()
            : _objectMapper.Map<SynchronizeTransactionInfoIndex, SynchronizeTransactionDto>(syncTxs.First());
    }

    public async Task<SynchronizeTransactionDto> GetSynchronizeJobBySymbolAsync(string symbol)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<SynchronizeTransactionInfoIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Symbol).Terms(symbol)));

        QueryContainer Filter(QueryContainerDescriptor<SynchronizeTransactionInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var (totalCount, syncTxs) = await _synchronizeTransactionRepository.GetListAsync(Filter);

        return totalCount < 1
            ? new SynchronizeTransactionDto()
            : _objectMapper.Map<SynchronizeTransactionInfoIndex, SynchronizeTransactionDto>(syncTxs.First());
    }
}