using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;
using GraphQL;
using GraphQL.Client.Abstractions;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using MassTransit.Caching.Internals;
using Microsoft.Extensions.Logging;
using NFTMarketServer.Common;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using Volo.Abp.Application.Dtos;
using Xunit;
using Xunit.Abstractions;

using File = System.IO.File;

namespace NFTMarketServer;

public class NftTest : NFTMarketServerApplicationTestBase
{
    private const string PrePath = "/Users/yanfeng/Desktop/forest_data/";
    private const string PathSold =  PrePath + "sold.json";
    
    private readonly List<SoldIndex> SoldIndices = new ();
    
    private readonly ITestOutputHelper _output;
    private readonly GraphQLHelper _graphQlHelper;
    private readonly IGraphQLClient _client;

    private readonly INFTInfoProvider _nftInfoProvider;
    private readonly ISeedInfoProvider _seedInfoProvider;
    private readonly INFTActivityProvider _nftActivityProvider;
    private readonly Dictionary<string, int> _soldUserDictionary = new Dictionary<string, int>();


    public NftTest(ITestOutputHelper testOutputHelper          
    ) : base(testOutputHelper)
    {
        _client = new GraphQLHttpClient(
            "https://dapp.eforest.finance/AElfIndexer_Forest/ForestIndexerPluginSchema/graphql",
            new NewtonsoftJsonSerializer());
        _graphQlHelper = new GraphQLHelper(_client, GetRequiredService<ILogger<GraphQLHelper>>());
        _nftInfoProvider = new NFTInfoProvider(_graphQlHelper);
        _seedInfoProvider = new SeedInfoProvider(_graphQlHelper);
        _output = testOutputHelper;
        
        //_nftActivityAppService = nftActivityAppService;
        _nftActivityProvider = new NFTActivityProvider((_graphQlHelper));

    }
    [Fact]
    public async void Test_AllSoldRecord()
    {
        int skipCount = 0;
        int maxResultCount = 2000;
        NFTActivityIndex result = null;
        var insAmount = 0.0;
        var notContainInsAmount = 0.0;

        var notContainInsNFTNumber = 0.0;

        List<string> inscriptions = new List<string>(){"tDVV-ELEPHANT-1","tDVV-DADDYCHILL-1","tDVV-CHEFCURRY-1","tDVV-NEWBEE-1" };
        do
        {
            var input = new GetActivitiesInput()
            {
                Types = new List<int>(){3},//query sold
                SkipCount = skipCount,
                MaxResultCount = maxResultCount
                ,TimestampMin = 1680307200000//UTC 2023-04-01 00:00:00 1680307200000, // 4.15 00:1713139200
                ,TimestampMax = 1713744000000//UTC 2024-04-22 00:00:00

            };
            result = await _nftActivityProvider.GetNFTActivityListAsync(null, input.Types, input.TimestampMin, input.TimestampMax, input.SkipCount, input.MaxResultCount);
            skipCount += result.IndexerNftActivity.Count;
            var activitys = result.IndexerNftActivity;
            foreach (var activity in activitys)
            {
                var isHave =  _soldUserDictionary.TryGetValue(activity.From, out var userCount);
                if (!isHave)
                {
                    _soldUserDictionary[activity.From] = 1;
                }
            }
            
            var insSolds = activitys.Where(item => inscriptions.Contains(item.NFTInfoId)).ToList();
            var notContainsInsSolds = activitys.Where(item => !inscriptions.Contains(item.NFTInfoId)).ToList();

            insAmount +=  (double)insSolds.Sum(x => (x.Amount * x.Price));
            notContainInsAmount  +=  (double)notContainsInsSolds.Sum(x => (x.Amount * x.Price));
            
            notContainInsNFTNumber +=  (double)notContainsInsSolds.Sum(x => x.Amount);

        } while (result != null && result.IndexerNftActivity.Count !=0);
        
        _output.WriteLine($"Test_AllSoldRecord insSoldAmount: {insAmount},notContainInsAmount:{notContainInsAmount},soldUser:{_soldUserDictionary.Count} ,notContainInsNFTNumber:{notContainInsNFTNumber}");
    }

    
    
    
    
    [Fact]
    public async void Test_Count()
    { 
        int nftCount = 0;
        int skipCout = 0;
        long supplyCount = 0;
        IndexerNFTInfos nftInfos = null;
        do
        {

            var nftInput = new GetNFTInfosProfileInput
            {
                IsSeed = false,
                SkipCount = skipCout,
                MaxResultCount = 1000
            };
            
            nftInfos = await _nftInfoProvider.GetNFTInfoIndexsUserProfileAsync(nftInput);
            
            if (nftInfos.IndexerNftInfos.IsNullOrEmpty())
            {
                return;
            }

            supplyCount +=  nftInfos.IndexerNftInfos.Sum(x => x.Supply);

            nftCount += nftInfos.IndexerNftInfos.Count;
            skipCout += nftInfos.IndexerNftInfos.Count;

        } while (!nftInfos.IndexerNftInfos.IsNullOrEmpty());

        _output.WriteLine($"nftCount: {nftCount}, supplyCount: {supplyCount}");
    }


    [Fact]
    public void Sold_Test()
    {
        LoadSoldData();
        var monthlyCounts = SoldIndices
            /*
            .Where(index => !index.CollectionSymbol.Contains("ELEPHANT") && !index.CollectionSymbol.Contains("DADDYCHILL")
                 && !index.CollectionSymbol.Contains("NEWBEE") && !index.CollectionSymbol.Contains("CHEFCURRY"))*/    
            .GroupBy(index => new { Year = index.DealTime.Year, Month = index.DealTime.Month })
            .Select(group =>
            {
                var uniqueAddressesSet = new HashSet<string>();
                uniqueAddressesSet.UnionWith(group.Select(s => s.NftFrom).ToHashSet());
                uniqueAddressesSet.UnionWith(group.Select(s => s.NftTo).ToHashSet());
                return new
                {
                    Year = group.Key.Year,
                    Month = group.Key.Month,
                    Count = group.Count(),
                    TotalTransAmount = group.Sum(x => x.PurchaseAmount),
                    TotalNFTAmount = group.Sum(x =>
                    {
                        if (x.CollectionSymbol.Contains("SGR"))
                        {
                            return FTHelper.GetRealELFAmount(long.Parse(x.NftQuantity));
                        }

                        return long.Parse(x.NftQuantity);
                    }),
                    UniqueAddresses = uniqueAddressesSet.Count
                };
            })
            .OrderBy(result => result.Year)
            .ThenBy(result => result.Month);
        foreach (var result in monthlyCounts)
        {
            _output.WriteLine($"Year: {result.Year}, Month: {result.Month}, Transactions: {result.Count}, Total Purchase Amount: {FTHelper.GetRealELFAmount(result.TotalTransAmount) }, Total NFT Amount: {result.TotalNFTAmount},  Unique Addresses: {result.UniqueAddresses}");
        }
    }
    
    private void LoadSoldData()
    {
        SoldIndices.Clear();
        string jsonData = System.IO.File.ReadAllText(PrePath + "sold.json");
        
        ParseSoldJson(jsonData);
    }


    [Fact]
    public async void Test_Seed_Count()
    { 
        int nftCount = 20000;
        int skipCout = 20000;
        IndexerSeedInfos nftInfos = null;
        do
        {
            var nftInput = new GetNFTInfosProfileInput
            {
                IsSeed = false,
                SkipCount = skipCout,
                MaxResultCount = 1000
            };
            
            nftInfos = await _seedInfoProvider.GetSeedInfosUserProfileAsync(nftInput);
            if (nftInfos.IndexerSeedInfoList.IsNullOrEmpty())
            {
                return;
            }
            nftCount += nftInfos.IndexerSeedInfoList.Count;
            skipCout += nftInfos.IndexerSeedInfoList.Count;

        } while (!nftInfos.IndexerSeedInfoList.IsNullOrEmpty());
        
        Console.WriteLine("nftCount: " + nftCount);

        //var dataDto = await GetNftSoldDataAsync(DateTime.UtcNow.AddDays(-30).Date, DateTime.UtcNow.Date);
    }


    private async Task<NFTSoldDataDto> GetNftSoldDataAsync(DateTime startTime, DateTime endTime)
    {
        var indexerCommonResult =
            await _graphQlHelper.QueryAsync<IndexerCommonResult<NFTSoldDataDto>>(new GraphQLRequest
            {
                Query = @"query($startTime:DateTime!,$endTime:DateTime!) {
                     nftSoldData(input:{startTime:$startTime,endTime:$endTime}) {
                        {
                             totalTransCount,
                             totalTransAmount,
                             totalAddressCount,
                             totalNftAmount
                        }
                    }
                  }",
                Variables = new
                {
                    startTime,
                    endTime
                }
            });
        return indexerCommonResult?.Data;
    }

    private class NFTSoldDataDto
    {
        public long TotalTransCount { get; set; }
    
        public long TotalTransAmount { get; set; }
    
        public long TotalAddressCount{ get; set; }
    
        public long TotalNftAmount { get; set; }
    }
    
    
    public class SoldIndex
    {
         public string NftFrom { get; set; }
         public string NftTo { get; set; }
         public string NftSymbol { get; set; }
         public string NftQuantity { get; set; }
         public string NftInfoId { get; set; }
         public long PurchaseAmount { get; set; }
         public DateTime DealTime { get; set; }
         public string CollectionSymbol { get; set; }
    }
    
    private void ParseSoldJson(string jsonString)
    {
        using (JsonDocument doc = JsonDocument.Parse(jsonString))
        {
            JsonElement root = doc.RootElement;
            JsonElement hitsArray = root.GetProperty("hits").GetProperty("hits");
            
            foreach (JsonElement hit in hitsArray.EnumerateArray())
            {
                JsonElement source = hit.GetProperty("_source");
                string collectionSymbol = source.GetProperty("collectionSymbol").GetString();
                string nftFrom = source.GetProperty("nftFrom").GetString();
                string nftTo =  source.GetProperty("nftTo").GetString();
                DateTime dealTime = source.GetProperty("dealTime").GetDateTime();
                string nftQuantity = source.GetProperty("nftQuantity").GetString();
                long purchaseAmount = source.GetProperty("purchaseAmount").GetInt64();
                
                SoldIndices.Add(new SoldIndex()
                {
                    CollectionSymbol = collectionSymbol,
                    NftFrom = nftFrom,
                    NftTo = nftTo,
                    NftQuantity = nftQuantity,
                    PurchaseAmount = purchaseAmount,
                    DealTime = dealTime
                });
            }
        }
    }

 
}