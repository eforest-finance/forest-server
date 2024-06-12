using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Ai;
using NFTMarketServer.Ai.Index;
using NFTMarketServer.Basic;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

internal class AIArtProvider : IAIArtProvider, ISingletonDependency
{
    private readonly INESTRepository<AIImageIndex, string> _aiImageIndexRepository;
    private readonly INESTRepository<AiCreateIndex, string> _aiCreateIndexRepository;

    public AIArtProvider(
        INESTRepository<AIImageIndex, string> aiImageIndexRepository
        , INESTRepository<AiCreateIndex, string> aiCreateIndexRepository)
    {
        _aiImageIndexRepository = aiImageIndexRepository;
        _aiCreateIndexRepository = aiCreateIndexRepository;
    }
    
    public async Task<Tuple<long, List<AIImageIndex>>> GetAIImageListAsync(SearchAIArtsInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AIImageIndex>, QueryContainer>>();
        if (!input.Address.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.Address).Value(input.Address)));
        }

        if (input.Status != (int)AiImageUseStatus.ALL)
        {
            mustQuery.Add(q => q.Term(i =>
                i.Field(f => f.status).Value(input.Status)));
        }

        if (!input.ImageIds.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(input.ImageIds)));
        }
        
        if (!input.ImageHash.IsNullOrEmpty())
        {
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Hash).Terms(input.ImageHash)));
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

    public async Task<Tuple<long, List<AiCreateIndex>>> GetFailAiCreateIndexListAsync(string address,
        QueryAiArtFailInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<AiCreateIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Term(i =>
            i.Field(f => f.Address).Value(address)));
        mustQuery.Add(q => q.Terms(i =>
            i.Field(f => f.Status).Terms(new List<AiCreateStatus>()
                { AiCreateStatus.PAYSUCCESS, AiCreateStatus.IMAGECREATED })));
        mustQuery.Add(q =>
            q.Range(i => i.Field(f => f.RetryCount).GreaterThanOrEquals(CommonConstant.IntThree)));

        QueryContainer Filter(QueryContainerDescriptor<AiCreateIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var sorting = new Func<SortDescriptor<AiCreateIndex>, IPromise<IList<ISort>>>(s =>
            s.Descending(t => t.Ctime));
        var tuple = await _aiCreateIndexRepository.GetSortListAsync(Filter, skip: input.SkipCount,
            limit: input.MaxResultCount,
            sortFunc: sorting);
        return tuple;
    }

    public async Task<AiCreateIndex> GetAiCreateIndexById(string id)
    {
        return await _aiCreateIndexRepository.GetAsync(id);
    }
}