using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.Basic;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Tokens;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Trait;

public class TraitInfoAppService : ITraitInfoAppService, ISingletonDependency
{
    private readonly INFTInfoNewSyncedProvider _nftInfoNewSyncedProvider;
    private readonly INFTCollectionProvider _nftCollectionProvider;
    private readonly ITraitInfoProvider _traitInfoProvider;
    private readonly IObjectMapper _objectMapper;

    public TraitInfoAppService(INFTInfoNewSyncedProvider nftInfoNewSyncedProvider,
        ITraitInfoProvider traitInfoProvider,
        INFTCollectionProvider nftCollectionProvider,
        IObjectMapper objectMapper)
    {
        _nftInfoNewSyncedProvider = nftInfoNewSyncedProvider;
        _traitInfoProvider = traitInfoProvider;
        _nftCollectionProvider = nftCollectionProvider;
        _objectMapper = objectMapper;
    }

    public async Task<NFTTraitsInfoDto> QueryNFTTraitsInfoAsync(QueryNFTTraitsInfoInput input)
    {
        var nftInfo = await _nftInfoNewSyncedProvider.GetNFTInfoIndexAsync(input.Id);
        if (nftInfo == null)
        {
            return new NFTTraitsInfoDto();
        }

        var result = _objectMapper.Map<IndexerNFTInfo, NFTTraitsInfoDto>(nftInfo);
        result.Id = input.Id;
        if (nftInfo.TraitPairsDictionary.IsNullOrEmpty())
        {
            return result;
        }

        var keyList = nftInfo.TraitPairsDictionary.Select(x => x.Key).ToList();

        var collectionTraitKeyInfos =
            await _traitInfoProvider.QueryCollectionTraitKeyInfosAsync(nftInfo.CollectionSymbol, keyList);

        var collectionTraitPairInfos =
            await _traitInfoProvider.QueryCollectionTraitPairInfosAsync(nftInfo.CollectionSymbol,
                nftInfo.TraitPairsDictionary);

        return BuildNFTTraitsInfoDto(nftInfo, keyList, collectionTraitKeyInfos, collectionTraitPairInfos);
    }


    public async Task<PagedResultDto<NFTCollectionTraitInfoDto>> QueryNFTCollectionTraitsInfoAsync(
        QueryNFTCollectionTraitsInfoInput input)
    {
        var result = new PagedResultDto<NFTCollectionTraitInfoDto>()
        {
            TotalCount = CommonConstant.IntZero,
            Items = new Collection<NFTCollectionTraitInfoDto>()
        };

        var nftCollectionInfo = await _nftCollectionProvider.GetNFTCollectionIndexAsync(input.Id);
        if (nftCollectionInfo == null)
        {
            return result;
        }

        var keyList = await _traitInfoProvider.QueryTraitKeyListByCollectionSymbolAsync(nftCollectionInfo.Symbol,
            input.SkipCount, input.MaxResultCount);
        if (keyList.IsNullOrEmpty())
        {
            return result;
        }

        if (keyList.IsNullOrEmpty())
        {
            return result;
        }

        var keyPairDic =
            await _traitInfoProvider.QueryCollectionTraitPairsInfoSortByKeyAsync(nftCollectionInfo.Symbol, keyList);
        if (keyPairDic.IsNullOrEmpty())
        {
            return result;
        }

        result.TotalCount = keyPairDic.Count;
        var infoList = new Collection<NFTCollectionTraitInfoDto>();
        foreach (var kvp in keyPairDic)
        {
            var valueList = kvp.Value;

            var info = new NFTCollectionTraitInfoDto
            {
                Key = kvp.Key,
                ValueCount = valueList.Count,
                values = new List<ValueCountDictionary>()
            };
            foreach (var traitPair in valueList)
            {
                var dic = new ValueCountDictionary();
                dic.Value = traitPair.TraitValue;
                dic.ItemsCount = traitPair.ItemCount;
                info.values.Add(dic);
            }

            infoList.Add(info);
        }

        return new PagedResultDto<NFTCollectionTraitInfoDto>()
        {
            TotalCount = CommonConstant.IntZero,
            Items = infoList
        };
    }

    public async Task<CollectionGenerationInfoDto> QueryCollectionGenerationInfoAsync(
        QueryCollectionGenerationInfoInput input)
    {
        var nftCollectionInfo = await _nftCollectionProvider.GetNFTCollectionIndexAsync(input.Id);
        if (nftCollectionInfo == null)
        {
            return new CollectionGenerationInfoDto();
        }

        var result = await _traitInfoProvider.QueryCollectionGenerationInfoAsync(nftCollectionInfo.Symbol);
        if (result.IsNullOrEmpty())
        {
            return new CollectionGenerationInfoDto();
        }

        return new CollectionGenerationInfoDto()
        {
            Id = nftCollectionInfo.Symbol,
            TotalCount = result.Count,
            Items = result.Select(kvp => new GenerationInfoDto
                { Generation = kvp.Key, GenerationItemsCount = kvp.Value }).ToList()
        };
    }

    private NFTTraitsInfoDto BuildNFTTraitsInfoDto(IndexerNFTInfo nftInfo, List<string> keyList,
        Dictionary<string, NFTCollectionTraitKeyIndex> nftCollectionTraitKeyDic,
        Dictionary<string, NFTCollectionTraitPairsIndex> nftCollectionTraitPairsDic)
    {
        var result = new NFTTraitsInfoDto();
        result.Generation = nftInfo.Generation;
        result.Id = nftInfo.Id;
        result.TraitInfos = new List<NFTTraitInfoDto>();
        if (keyList.IsNullOrEmpty())
        {
            return result;
        }

        foreach (var key in keyList)
        {
            var nftTraitInfoDto = new NFTTraitInfoDto();
            if (nftCollectionTraitKeyDic.TryGetValue(key, out var value))
            {
                nftTraitInfoDto.AllItemsCount = value.ItemCount;
            }

            if (nftCollectionTraitPairsDic.TryGetValue(key, out var dic))
            {
                nftTraitInfoDto.Id = dic.Id;
                nftTraitInfoDto.Key = dic.TraitKey;
                nftTraitInfoDto.Value = dic.TraitValue;
                nftTraitInfoDto.ItemsCount = dic.ItemCount;
                if (dic.ItemFloorPrice > CommonConstant.IntZero)
                {
                    nftTraitInfoDto.ItemFloorPrice = dic.ItemFloorPrice;
                    nftTraitInfoDto.ItemFloorPriceToken =
                        _objectMapper.Map<IndexerTokenInfo, TokenDto>(
                            _objectMapper.Map<TokenInfoIndex, IndexerTokenInfo>(dic.FloorPriceToken));
                }
                if (dic.ItemLatestDealPrice > CommonConstant.IntZero)
                {
                    nftTraitInfoDto.LatestDealPrice = dic.ItemLatestDealPrice;
                }
            }

            result.TraitInfos.Add(nftTraitInfoDto);
        }

        return result;
    }
}