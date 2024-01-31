using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public class NFTDropExtensionProvider :  INFTDropExtensionProvider, ISingletonDependency
{
    private readonly INESTRepository<NFTDropExtensionIndex, string> _nftDropExtensionIndexRepository;
    
    public NFTDropExtensionProvider(
        INESTRepository<NFTDropExtensionIndex, string> nftDropExtensionIndexRepository)
    {
        _nftDropExtensionIndexRepository = nftDropExtensionIndexRepository;
    }
    
    public async Task<Dictionary<string, NFTDropExtensionIndex>> BatchGetNFTDropExtensionAsync(List<string> dropIds)
    {
        var result = new Dictionary<string, NFTDropExtensionIndex>();
        if (dropIds == null)
        {
            return result;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTDropExtensionIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id)
            .Terms(dropIds)));

        QueryContainer Filter(QueryContainerDescriptor<NFTDropExtensionIndex> f) =>
            f.Bool(b => b.Must(mustQuery));

        var extensions =
            await _nftDropExtensionIndexRepository.GetListAsync(Filter);
        if (extensions == null || extensions.Item2 == null)
        {
            return result;
        }

        foreach (NFTDropExtensionIndex item in extensions.Item2)
        {
            result.Add(item.Id, item);
        }

        return result;
    }
}