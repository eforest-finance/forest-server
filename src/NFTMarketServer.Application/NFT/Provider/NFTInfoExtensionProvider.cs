using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.NFT.Index;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.NFT.Provider;

public class NFTInfoExtensionProvider : INFTInfoExtensionProvider, ISingletonDependency
{
    private readonly INESTRepository<NFTInfoExtensionIndex, string> _nftInfoExtensionIndexRepository;

    public NFTInfoExtensionProvider(INESTRepository<NFTInfoExtensionIndex, string> nftInfoExtensionIndexRepository)
    {
        _nftInfoExtensionIndexRepository = nftInfoExtensionIndexRepository;
    }


    public async Task<Dictionary<string, NFTInfoExtensionIndex>> GetNFTInfoExtensionsAsync(List<string> nftInfoExtensionIndexIds)
    {
        var result = new Dictionary<string, NFTInfoExtensionIndex>();
        if (nftInfoExtensionIndexIds == null)
        {
            return result;
        }

        var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoExtensionIndex>, QueryContainer>>();
        mustQuery.Add(q => q.Terms(i => i.Field(f => f.Id)
            .Terms(nftInfoExtensionIndexIds)));

        QueryContainer Filter(QueryContainerDescriptor<NFTInfoExtensionIndex> f) => f.Bool(b => b.Must(mustQuery));
        var extensions =
            await _nftInfoExtensionIndexRepository.GetListAsync(Filter);
        if (extensions == null || extensions.Item2 == null)
        {
            return result;
        }
        foreach(NFTInfoExtensionIndex tem in extensions.Item2)
        {
            result.Add(tem.Id,tem);   
        }

        return result;
    }
}