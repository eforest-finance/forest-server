using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Nest;
using Newtonsoft.Json;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Entities;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.Tokens;
using Orleans.Runtime;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT.Provider;

public interface INFTTraitProvider
{
    Task CheckAndUpdateTraitInfo(NFTInfoNewIndex nftInfoNewIndex);
    
    Task<long> QueryItemCountForNFTCollectionWithTraitKeyAsync(string key,
        string nftCollectionId);

    Task<long> QueryItemCountForNFTCollectionWithTraitPairAsync(string key, string value,
        string nftCollectionId);

    Task<long> QueryItemCountForNFTCollectionGenerationAsync(string nftCollectionId, int generation);

    Task<NFTInfoNewIndex> QueryFloorPriceNFTForNFTWithTraitPair(string key, string value,
        string nftCollectionId);
    
    Task CheckAndUpdateRarityInfo(NFTInfoNewIndex nftInfoNewIndex);

}

public class NFTTraitProvider : INFTTraitProvider, ISingletonDependency
{
    private readonly INESTRepository<NFTCollectionTraitKeyIndex, string> _nftCollectionTraitKeyIndexRepository;
    private readonly INESTRepository<NFTCollectionTraitPairsIndex, string> _nftCollectionTraitPairsIndexRepository;
    private readonly INESTRepository<NFTCollectionTraitGenerationIndex, string>
        _nftCollectionTraitGenerationIndexRepository;
    private readonly INESTRepository<NFTCollectionRarityIndex, string>
        _nftCollectionRarityIndexRepository;
    private readonly INESTRepository<NFTInfoNewIndex, string> _nftInfoNewIndexRepository;
    private readonly ILogger<NFTTraitProvider> _logger;
    private readonly IObjectMapper _objectMapper;

    public NFTTraitProvider(
        ILogger<NFTTraitProvider> logger,
        INESTRepository<NFTCollectionTraitKeyIndex, string> nftCollectionTraitKeyIndexRepository,
        INESTRepository<NFTCollectionTraitPairsIndex, string> nftCollectionTraitPairsIndexRepository,
        INESTRepository<NFTCollectionTraitGenerationIndex, string> nftCollectionTraitGenerationIndexRepository,
        INESTRepository<NFTCollectionRarityIndex, string> nftCollectionRarityIndexRepository,
        INESTRepository<NFTInfoNewIndex, string> nftInfoNewIndexRepository,
        IObjectMapper objectMapper
    )
    {
        _logger = logger;
        _nftCollectionTraitKeyIndexRepository = nftCollectionTraitKeyIndexRepository;
        _nftCollectionTraitPairsIndexRepository = nftCollectionTraitPairsIndexRepository;
        _nftCollectionTraitGenerationIndexRepository = nftCollectionTraitGenerationIndexRepository;
        _nftInfoNewIndexRepository = nftInfoNewIndexRepository;
        _objectMapper = objectMapper;
        _nftCollectionRarityIndexRepository = nftCollectionRarityIndexRepository;
    }
    
    public async Task<long> QueryItemCountForNFTCollectionWithTraitKeyAsync(string key,
            string nftCollectionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q =>
                q.Term(i => i.Field(f => f.CountedFlag).Value(true)));

            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            nestedQuery.Add(q => q
                .Nested(n => n
                    .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                    .Query(nq => nq
                        .Bool(nb => nb
                            .Must(nm => nm
                                .Match(m => m
                                    .Field(f => f.TraitPairsDictionary.First().Key)
                                    .Query(key)
                                )
                            )
                        )
                    )
                )
            );
            mustQuery.AddRange(nestedQuery);

            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, skip: CommonConstant.IntZero,
                limit: CommonConstant.IntZero);
            if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
            {
                return result.Item1;
            }

            return await QueryRealCountAsync(mustQuery);
        }

        public async Task<long> QueryItemCountForNFTCollectionWithTraitPairAsync(string key, string value,
            string nftCollectionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q =>
                q.Term(i => i.Field(f => f.CountedFlag).Value(true)));

            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            nestedQuery.Add(q => q
                .Nested(n => n
                    .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                    .Query(nq => nq
                        .Bool(nb => nb
                            .Must(nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Key)
                                        .Query(key)
                                    ),
                                nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Value)
                                        .Query(value)
                                    )
                            )
                        )
                    )
                )
            );
            mustQuery.AddRange(nestedQuery);

            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, skip: CommonConstant.IntZero,
                limit: CommonConstant.IntZero);
            if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
            {
                return result.Item1;
            }

            return await QueryRealCountAsync(mustQuery);
        }


        public async Task<long> QueryItemCountForNFTCollectionGenerationAsync(string nftCollectionId,
            int generation)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.Generation).Value(generation)));
            mustQuery.Add(q =>
                q.Term(i => i.Field(f => f.CountedFlag).Value(true)));
            
            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetSortListAsync(Filter, skip: CommonConstant.IntZero,
                limit: CommonConstant.IntZero);
            if (result?.Item1 != null && result?.Item1 != CommonConstant.EsLimitTotalNumber)
            {
                return result.Item1;
            }

            return await QueryRealCountAsync(mustQuery);
        }

        public async Task<NFTInfoNewIndex> QueryFloorPriceNFTForNFTWithTraitPair(string key, string value,
            string nftCollectionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.Supply).GreaterThan(CommonConstant.IntZero)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.ListingPrice).GreaterThan(CommonConstant.IntZero)));

            var nowStr = DateTime.UtcNow.ToString("o");
            mustQuery.Add(q => q.DateRange(i => i.Field(f => f.ListingEndTime).GreaterThan(nowStr)));

            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            nestedQuery.Add(q => q
                .Nested(n => n
                    .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                    .Query(nq => nq
                        .Bool(nb => nb
                            .Must(nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Key)
                                        .Query(key)
                                    ),
                                nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Value)
                                        .Query(value)
                                    )
                            ))
                    )
                )
            );

            mustQuery.AddRange(nestedQuery);

            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetListAsync(Filter
                ,skip: CommonConstant.IntZero,
                limit: CommonConstant.IntOne,
                sortType: SortOrder.Ascending, sortExp: o => o.ListingPrice);
            return result?.Item2?.FirstOrDefault();
        }

        public async Task CheckAndUpdateRarityInfo(NFTInfoNewIndex nftInfoNewIndex)
        {
            if (nftInfoNewIndex == null)
            {
                return;
            }
        
            await CheckAndUpdateNFTCollectionTraitGenerationIndexInfo(nftInfoNewIndex);
        }


        public async Task<NFTInfoNewIndex> QueryLatestDealPriceNFTForNFTWithTraitPair(string key, string value,
            string nftCollectionId)
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            mustQuery.Add(q => q.Term(i => i.Field(f => f.CollectionId).Value(nftCollectionId)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.Supply).GreaterThan(CommonConstant.IntZero)));
            mustQuery.Add(q =>
                q.Range(i => i.Field(f => f.LatestDealPrice).GreaterThan(CommonConstant.IntZero)));

            var nestedQuery = new List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>>();
            nestedQuery.Add(q => q
                .Nested(n => n
                    .Path(CommonConstant.ES_NFT_TraitPairsDictionary_Path)
                    .Query(nq => nq
                        .Bool(nb => nb
                            .Must(nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Key)
                                        .Query(key)
                                    ),
                                nm => nm
                                    .Match(m => m
                                        .Field(f => f.TraitPairsDictionary.First().Value)
                                        .Query(value)
                                    )
                            ))
                    )
                )
            );

            mustQuery.AddRange(nestedQuery);

            QueryContainer Filter(QueryContainerDescriptor<NFTInfoNewIndex> f)
                => f.Bool(b => b.Must(mustQuery));

            var result = await _nftInfoNewIndexRepository.GetListAsync(Filter
                ,skip: CommonConstant.IntZero,
                limit: CommonConstant.IntOne,
                sortType: SortOrder.Descending, sortExp: o => o.LatestDealTime);
            return result?.Item2?.FirstOrDefault();
        }


    public async Task CheckAndUpdateTraitInfo(NFTInfoNewIndex nftInfoNewIndex)
    {
        if (nftInfoNewIndex == null)
        {
            return;
        }
        
        await CheckAndUpdateNFTCollectionTraitGenerationIndexInfo(nftInfoNewIndex);

        if (nftInfoNewIndex.TraitPairsDictionary.IsNullOrEmpty())
        {
            return;
        }
        
        foreach (var item in nftInfoNewIndex.TraitPairsDictionary)
        {
            if (item == null || string.IsNullOrEmpty(item.Key) || string.IsNullOrEmpty(item.Value))
            {
                continue;
            }

            await CheckAndUpdateNFTCollectionTraitKeyIndexInfo(nftInfoNewIndex, item);
            await CheckAndUpdateNFTCollectionTraitPairsIndexInfo(nftInfoNewIndex, item);
        }

    }

    private async Task<NFTCollectionTraitKeyIndex> QueryNFTCollectionTraitKeyIndexById(string id)
    {
        return await _nftCollectionTraitKeyIndexRepository.GetAsync(id);
    }

    private async Task<NFTCollectionTraitPairsIndex> QueryNFTCollectionTraitPairsIndexById(string id)
    {
        return await _nftCollectionTraitPairsIndexRepository.GetAsync(id);
    }

    public async Task<NFTCollectionTraitGenerationIndex> QueryNFTCollectionTraitGenerationIndexById(string id)
    {
        return await _nftCollectionTraitGenerationIndexRepository.GetAsync(id);
    }
    
    public async Task<NFTCollectionRarityIndex> QueryNFTCollectionRarityIndexById(string id)
    {
        return await _nftCollectionRarityIndexRepository.GetAsync(id);
    }

    private async Task CheckAndUpdateNFTCollectionTraitKeyIndexInfo(NFTInfoNewIndex nftInfoNewIndex,
        ExternalInfoDictionary trait)
    {
        var id = IdGenerateHelper.GetNFTCollectionTraitKeyId(nftInfoNewIndex.CollectionSymbol, trait.Key);
        var nftCollectionTraitKeyIndex = await QueryNFTCollectionTraitKeyIndexById(id);

        if (nftCollectionTraitKeyIndex == null)
        {
            nftCollectionTraitKeyIndex = new NFTCollectionTraitKeyIndex
            {
                Id = id,
                NFTCollectionSymbol = nftInfoNewIndex.CollectionSymbol,
                TraitKey = trait.Key,
                ItemCount = FTHelper.IsGreaterThanEqualToOne(nftInfoNewIndex.Supply, nftInfoNewIndex.Decimals)
                    ? CommonConstant.LongOne
                    : CommonConstant.IntZero,
            };
        }

        var newCount = await QueryItemCountForNFTCollectionWithTraitKeyAsync(trait.Key,
            nftInfoNewIndex.CollectionId);
        if (nftCollectionTraitKeyIndex.ItemCount == newCount)
        {
            return;
        }

        nftCollectionTraitKeyIndex.ItemCount = newCount;

        await _nftCollectionTraitKeyIndexRepository.AddOrUpdateAsync(nftCollectionTraitKeyIndex);
    }

    private async Task CheckAndUpdateNFTCollectionTraitPairsIndexInfo(NFTInfoNewIndex nftInfoNewIndex,
        ExternalInfoDictionary trait)
    {
        var id = IdGenerateHelper.GetNFTCollectionTraitPairsId(nftInfoNewIndex.CollectionSymbol, trait.Key,
            trait.Value);
        var nftCollectionTraitPairsIndex = await QueryNFTCollectionTraitPairsIndexById(id);
        if (nftCollectionTraitPairsIndex == null)
        {
            nftCollectionTraitPairsIndex = new NFTCollectionTraitPairsIndex
            {
                Id = id,
                NFTCollectionSymbol = nftInfoNewIndex.CollectionSymbol,
                TraitKey = trait.Key,
                TraitValue = trait.Value,
                ItemCount = FTHelper.IsGreaterThanEqualToOne(nftInfoNewIndex.Supply, nftInfoNewIndex.Decimals)
                    ? CommonConstant.LongOne
                    : CommonConstant.IntZero, 
                ItemFloorPrice= CommonConstant.DefaultValueNone,
                FloorPriceToken = new TokenInfoIndex
                {
                    Decimals = CommonConstant.Coin_ELF_Decimals,
                    Symbol = CommonConstant.Coin_ELF
                },
                FloorPriceNFTSymbol = "",
                ItemLatestDealPrice = CommonConstant.DefaultValueNone
            };
        }

        var changeFlag = false;
        var newItemCount = await QueryItemCountForNFTCollectionWithTraitPairAsync(trait.Key,
            trait.Value,
            nftInfoNewIndex.CollectionId);
        
        if (nftCollectionTraitPairsIndex.ItemCount != newItemCount)
        {
            changeFlag = true;
            nftCollectionTraitPairsIndex.ItemCount = newItemCount;

        }

        var floorPriceNFT = await QueryFloorPriceNFTForNFTWithTraitPair(trait.Key,
            trait.Value,
            nftInfoNewIndex.CollectionId);

        if (floorPriceNFT == null || floorPriceNFT.HasListingFlag == null)
        {
            changeFlag = true;
            nftCollectionTraitPairsIndex.FloorPriceNFTSymbol = "";
            nftCollectionTraitPairsIndex.ItemFloorPrice = CommonConstant.DefaultValueNone;
        }
        else if (floorPriceNFT != null && !floorPriceNFT.HasListingFlag && nftCollectionTraitPairsIndex.ItemFloorPrice > CommonConstant.IntZero)
        {
            changeFlag = true;
            nftCollectionTraitPairsIndex.FloorPriceNFTSymbol = "";
            nftCollectionTraitPairsIndex.ItemFloorPrice = CommonConstant.DefaultValueNone;
        }
        else if (floorPriceNFT != null && floorPriceNFT?.ListingPrice != nftCollectionTraitPairsIndex.ItemFloorPrice)
        {
            changeFlag = true;
            nftCollectionTraitPairsIndex.FloorPriceNFTSymbol = floorPriceNFT.Symbol;
            nftCollectionTraitPairsIndex.FloorPriceToken = floorPriceNFT.ListingToken;
            nftCollectionTraitPairsIndex.ItemFloorPrice = floorPriceNFT.ListingPrice;
        }
        
        var latestDealPriceNFT = await QueryLatestDealPriceNFTForNFTWithTraitPair(trait.Key,
            trait.Value,
            nftInfoNewIndex.CollectionId);
        
        if (latestDealPriceNFT != null && 
            latestDealPriceNFT.LatestDealPrice != CommonConstant.IntZero
            &&
            nftCollectionTraitPairsIndex.ItemLatestDealPrice != latestDealPriceNFT.LatestDealPrice) 
        {
            changeFlag = true;
            nftCollectionTraitPairsIndex.ItemLatestDealPrice = latestDealPriceNFT.LatestDealPrice;
        }
        
        if (changeFlag)
        {
            await _nftCollectionTraitPairsIndexRepository.AddOrUpdateAsync(nftCollectionTraitPairsIndex);
        }
    }

    private async Task CheckAndUpdateNFTCollectionTraitGenerationIndexInfo(NFTInfoNewIndex nftInfoNewIndex)
    {
        if (nftInfoNewIndex == null)
        {
            return;
        }

        var id = IdGenerateHelper.GetNFTCollectionTraitGenerationId(nftInfoNewIndex.CollectionSymbol,
            nftInfoNewIndex.Generation);
        var nftCollectionTraitGenerationIndex = await QueryNFTCollectionTraitGenerationIndexById(id);
        if (nftCollectionTraitGenerationIndex == null)
        {
            nftCollectionTraitGenerationIndex = new NFTCollectionTraitGenerationIndex()
            {
                Id = id,
                CollectionSymbol = nftInfoNewIndex.CollectionSymbol,
                ItemCount = FTHelper.IsGreaterThanEqualToOne(nftInfoNewIndex.Supply, nftInfoNewIndex.Decimals)
                    ? CommonConstant.LongOne
                    : CommonConstant.IntZero,
                Generation = nftInfoNewIndex.Generation
            };
        }
        
        var newCount = await QueryItemCountForNFTCollectionGenerationAsync(
            nftInfoNewIndex.CollectionId, nftInfoNewIndex.Generation);
        
        if (nftCollectionTraitGenerationIndex.ItemCount == newCount && newCount != CommonConstant.IntZero)
        {
            return;
        }
        
        nftCollectionTraitGenerationIndex.ItemCount = newCount;

        await _nftCollectionTraitGenerationIndexRepository.AddOrUpdateAsync(nftCollectionTraitGenerationIndex);
    }
    
    private async Task CheckAndUpdateNFTCollectionRarityIndexInfo(NFTInfoNewIndex nftInfoNewIndex)
    {
        if (nftInfoNewIndex == null)
        {
            return;
        }

        var id = IdGenerateHelper.GetNFTCollectionRarityId(nftInfoNewIndex.CollectionSymbol,
            nftInfoNewIndex.Rarity);
        var nftCollectionRarityIndex = await QueryNFTCollectionRarityIndexById(id);
        if (nftCollectionRarityIndex == null)
        {
            nftCollectionRarityIndex = new NFTCollectionRarityIndex()
            {
                Id = id,
                CollectionSymbol = nftInfoNewIndex.CollectionSymbol,
                ItemCount = FTHelper.IsGreaterThanEqualToOne(nftInfoNewIndex.Supply, nftInfoNewIndex.Decimals)
                    ? CommonConstant.LongOne
                    : CommonConstant.IntZero,
                Rarity = nftInfoNewIndex.Rarity
            };
        }
        
        var newCount = await QueryItemCountForNFTCollectionGenerationAsync(
            nftInfoNewIndex.CollectionId, nftInfoNewIndex.Generation);
        
        if (nftCollectionRarityIndex.ItemCount == newCount && newCount != CommonConstant.IntZero)
        {
            return;
        }
        
        nftCollectionRarityIndex.ItemCount = newCount;

        await _nftCollectionRarityIndexRepository.AddOrUpdateAsync(nftCollectionRarityIndex);
    }
    
    private async Task<long> QueryRealCountAsync(
        List<Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer>> mustQuery)
    {
        var countRequest = new SearchRequest<NFTInfoIndex>
        {
            Query = new BoolQuery
            {
                Must = mustQuery != null && mustQuery.Any()
                    ? mustQuery.Select(func => func(new QueryContainerDescriptor<NFTInfoNewIndex>()))
                        .ToList()
                        .AsEnumerable()
                    : Enumerable.Empty<QueryContainer>()
            },
            Size = 0
        };

        Func<QueryContainerDescriptor<NFTInfoNewIndex>, QueryContainer> queryFunc = q => countRequest.Query;
        var realCount = await _nftInfoNewIndexRepository.CountAsync(queryFunc);
        return realCount.Count;
    }
}
