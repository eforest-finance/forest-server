using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Ai.Index;
using NFTMarketServer.Common;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public class AIArtProvider : IAIArtProvider, ISingletonDependency
{
    private readonly IGraphQLHelper _graphQlHelper;
    private readonly INESTRepository<AIImageIndex, string> _aiImageIndexRepository;


    public AIArtProvider(IGraphQLHelper graphQlHelper,
        INESTRepository<AIImageIndex, string> aiImageIndexRepository)
    {
        _graphQlHelper = graphQlHelper;
        _aiImageIndexRepository = aiImageIndexRepository;

    }
    
    public async Task<Tuple<long, List<AIImageIndex>>> GetAIImageListAsync(SearchAIArtsInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AIImageIndex>, QueryContainer>>();
        if (!input.Address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(input.Address)));
        }
        
        QueryContainer Filter(QueryContainerDescriptor<AIImageIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        
        var sorting = new Func<SortDescriptor<AIImageIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.Ctime));
        
        var tuple = await _aiImageIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: sorting);
        return tuple;

    }
}