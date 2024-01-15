using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using AElf.Indexing.Elasticsearch;
using NFTMarketServer.Basic;
using NFTMarketServer.Chain;
using NFTMarketServer.Chains;
using NFTMarketServer.Common;
using NFTMarketServer.Provider;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Seed;
using NFTMarketServer.Seed.Index;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Options;

namespace NFTMarketServer.NFT;

public class ExpiredNftMaxOfferSyncDataService : ScheduleSyncDataService
{
    private readonly ILogger<ScheduleSyncDataService> _logger;
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly INFTInfoAppService _nftInfoAppService;
    private readonly IChainAppService _chainAppService;
    private readonly INFTOfferProvider _nftOfferProvider;
    private readonly ISeedAppService _seedAppService;
    private readonly INESTRepository<NFTInfoIndex, string> _nftInfoIndexRepository;
    private readonly INESTRepository<SeedSymbolIndex, string> _seedSymbolIndexRepository;
    private readonly IOptionsMonitor<ExpiredNFTSyncOptions> _optionsMonitor;
    
    public ExpiredNftMaxOfferSyncDataService(ILogger<NftInfoSyncDataService> logger,
        IGraphQLProvider graphQlProvider,
        INFTInfoAppService nftInfoAppService, 
        IChainAppService chainAppService,
        INFTOfferProvider nftOfferProvider,
        ISeedAppService seedAppService,
        INESTRepository<NFTInfoIndex, string> nftInfoIndexRepository, 
        INESTRepository<SeedSymbolIndex, string> seedSymbolIndexRepository,
        IOptionsMonitor<ExpiredNFTSyncOptions> optionsMonitor)
        : base(logger, graphQlProvider, chainAppService)
    {
        _logger = logger;
        _graphQlProvider = graphQlProvider;
        _nftInfoAppService = nftInfoAppService;
        _chainAppService = chainAppService;
        _nftOfferProvider = nftOfferProvider;
        _seedAppService = seedAppService;
        _nftInfoIndexRepository = nftInfoIndexRepository;
        _seedSymbolIndexRepository = seedSymbolIndexRepository;
        _optionsMonitor = optionsMonitor;
    }
    
    public override async Task<long> SyncIndexerRecordsAsync(string chainId, long lastEndHeight, long newIndexHeight)
    {
        var option = _optionsMonitor.CurrentValue;
        var list = await _nftOfferProvider.GetNftMaxOfferAsync(chainId, option.Duration);
        _logger.LogInformation("GetNftMaxOffer, duration: {duration}", option.Duration);
        long blockHeight = -1;
        if (list.IsNullOrEmpty())
        {
            return 0;
        }

        foreach (var data in list)
        {
            var nftInfoId = data.Key;
            var isSeed = nftInfoId.Match(NFTSymbolBasicConstants.SeedIdPattern);
            if (isSeed)
            {
                var seedSymbol = await _seedSymbolIndexRepository.GetAsync(nftInfoId);
                var maxOfferInfo = data.Value;
                seedSymbol.HasOfferFlag = maxOfferInfo != null;
                seedSymbol.MaxOfferPrice = maxOfferInfo?.Prices ?? 0;
                seedSymbol.MaxOfferExpireTime = maxOfferInfo?.ExpireTime;
                seedSymbol.MaxOfferId = maxOfferInfo?.Id;
                
                await _seedAppService.AddOrUpdateSeedSymbolAsync(seedSymbol);
            }
            else
            {
                var nftInfo = await _nftInfoIndexRepository.GetAsync(nftInfoId);
                var maxOfferInfo = data.Value;
                nftInfo.HasOfferFlag = maxOfferInfo != null;
                nftInfo.MaxOfferPrice = maxOfferInfo?.Prices ?? 0;
                nftInfo.MaxOfferExpireTime = maxOfferInfo?.ExpireTime;
                nftInfo.MaxOfferId = maxOfferInfo?.Id;
                
                await _nftInfoAppService.AddOrUpdateNftInfoAsync(nftInfo);
            }
        }

        return blockHeight;
    }

    public override async Task<List<string>> GetChainIdsAsync()
    {
        //add multiple chains
        var chainIds = await _chainAppService.GetListAsync();
        return chainIds.ToList();
    }

    public override BusinessQueryChainType GetBusinessType()
    {
        return BusinessQueryChainType.ExpiredNftMaxOfferSync;
    }
}