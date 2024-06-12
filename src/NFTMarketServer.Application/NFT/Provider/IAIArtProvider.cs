using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.Ai;
using NFTMarketServer.Ai.Index;

namespace NFTMarketServer.NFT.Provider;

public interface IAIArtProvider
{
    Task<Tuple<long, List<AIImageIndex>>>  GetAIImageListAsync(SearchAIArtsInput input);

    Task<Tuple<long, List<AiCreateIndex>>> GetFailAiCreateIndexListAsync(string address, QueryAiArtFailInput input);

    Task<AiCreateIndex> GetAiCreateIndexById(string id);
}