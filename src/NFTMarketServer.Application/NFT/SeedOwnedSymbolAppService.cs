using System.Collections.Generic;
using System.Threading.Tasks;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT;

[RemoteService(IsEnabled = false)]
public class SeedOwnedSymbolAppService : NFTMarketServerAppService, ISeedOwnedSymbolAppService
{
    private readonly ISeedOwnedSymboProvider _seedOwnedSymboProvider;
    private readonly IObjectMapper _objectMapper;

    public SeedOwnedSymbolAppService(ISeedOwnedSymboProvider seedOwnedSymboProvider
        , IObjectMapper objectMapper)
    {
        _seedOwnedSymboProvider = seedOwnedSymboProvider;
        _objectMapper = objectMapper;
    }

    
    public async Task<PagedResultDto<SeedSymbolIndexDto>> GetSeedOwnedSymbolsAsync(GetSeedOwnedSymbols input)
    {
        if (input.SkipCount < 0) return buildInitSeedSymbolIndexDto();
        var seedOwnedSymbol = input.Symbol;
        var seedOwnedSymbolIndexs =
            await _seedOwnedSymboProvider.GetSeedOwnedSymbolsIndexAsync(input.SkipCount,
                input.MaxResultCount, input.Address, seedOwnedSymbol);
        if (seedOwnedSymbolIndexs == null) return buildInitSeedSymbolIndexDto();

        var totalCount = seedOwnedSymbolIndexs.TotalRecordCount;
        if (seedOwnedSymbolIndexs.IndexerSeedOwnedSymbolList == null)
        {
            return buildInitSeedSymbolIndexDto();
        }

        List<SeedSymbolIndexDto> symbolIndexDtos = _objectMapper.Map<List<IndexerSeedOwnedSymbol>
            , List<SeedSymbolIndexDto>>(seedOwnedSymbolIndexs.IndexerSeedOwnedSymbolList);

        return new PagedResultDto<SeedSymbolIndexDto>
        {
            Items = symbolIndexDtos,
            TotalCount = totalCount
        };
    }
    
    public async Task<PagedResultDto<SeedSymbolIndexDto>> GetAllSeedOwnedSymbolsAsync(GetAllSeedOwnedSymbols input)
    {
        if (input.SkipCount < 0) return buildInitSeedSymbolIndexDto();
        var seedOwnedSymbol = input.SeedOwnedSymbol;
        var seedOwnedSymbolIndexs =
            await _seedOwnedSymboProvider.GetAllSeedOwnedSymbolsIndexAsync(input.SkipCount,
                input.MaxResultCount, input.AddressList, seedOwnedSymbol);
        if (seedOwnedSymbolIndexs == null) return buildInitSeedSymbolIndexDto();

        var totalCount = seedOwnedSymbolIndexs.TotalRecordCount;
        if (seedOwnedSymbolIndexs.IndexerSeedOwnedSymbolList == null)
        {
            return buildInitSeedSymbolIndexDto();
        }

        List<SeedSymbolIndexDto> symbolIndexDtos = _objectMapper.Map<List<IndexerSeedOwnedSymbol>
            , List<SeedSymbolIndexDto>>(seedOwnedSymbolIndexs.IndexerSeedOwnedSymbolList);

        return new PagedResultDto<SeedSymbolIndexDto>
        {
            Items = symbolIndexDtos,
            TotalCount = totalCount
        };
    }

    private PagedResultDto<SeedSymbolIndexDto> buildInitSeedSymbolIndexDto()
    {
        return new PagedResultDto<SeedSymbolIndexDto>
        {
            Items = new List<SeedSymbolIndexDto>(),
            TotalCount = 0
        };
    }
}