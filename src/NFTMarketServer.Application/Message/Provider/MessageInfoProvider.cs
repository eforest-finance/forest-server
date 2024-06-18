using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Basic;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.Message.Provider;

public class MessageInfoProvider : IMessageInfoProvider,ISingletonDependency
{
    private readonly INESTRepository<MessageInfoIndex, string> _messageInfoIndexRepository;
    public MessageInfoProvider(
        INESTRepository<MessageInfoIndex, string> messageInfoIndexRepository)
    {
        _messageInfoIndexRepository = messageInfoIndexRepository;
    }
    public async Task<Tuple<long, List<MessageInfoIndex>>> GetUserMessageInfosAsync(string address, QueryMessageListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<MessageInfoIndex>, QueryContainer>>();
        if (!address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(address)));
        }

        if (input.Status == CommonConstant.MessageReadStatus || input.Status == CommonConstant.MessageUnReadStatus)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Status).Value(input.Status)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<MessageInfoIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<MessageInfoIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.Ctime));
        var tuple = await _messageInfoIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: sorting);
        return tuple;
    }
}