using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Ai.Index;

namespace NFTMarketServer.NFT.Provider;

public interface IAIArtProvider
{
    Task<Tuple<long, List<AIImageIndex>>>  GetAIImageListAsync(SearchAIArtsInput input);
    
}