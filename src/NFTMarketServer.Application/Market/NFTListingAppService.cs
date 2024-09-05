using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AElf;
using AElf.Client.Dto;
using AElf.Client.Service;
using AElf.Types;
using Forest;
using Google.Protobuf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using NFTMarketServer.Ai;
using NFTMarketServer.Basic;
using NFTMarketServer.Common;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Market
{
    [RemoteService(IsEnabled = false)]
    public class NFTListingAppService : NFTMarketServerAppService, INFTListingAppService
    {
        private readonly ILogger<NFTListingAppService> _logger;
        private readonly IUserAppService _userAppService;
        private readonly INFTListingProvider _nftListingProvider;
        private readonly IObjectMapper _objectMapper;
        private readonly ICompositeNFTProvider _compositeNFTProvider;
        private readonly INFTCollectionExtensionProvider _nftCollectionExtensionProvider;
        private readonly IOptionsMonitor<StatisticsUserListRecordOptions> _statisticsOptionsMonitor;
        private readonly IBlockchainClientFactory<AElfClient> _blockchainClientFactory;
        private readonly IOptionsMonitor<ChainOptions> _chainOptionsMonitor;



        public NFTListingAppService(IUserAppService userAppService,
            INFTListingProvider nftListingProvider,
            ILogger<NFTListingAppService> logger, 
            IObjectMapper objectMapper,
                ICompositeNFTProvider compositeNFTProvider,
            INFTCollectionExtensionProvider nftCollectionExtensionProvider,
            IOptionsMonitor<StatisticsUserListRecordOptions> statisticsOptionsMonitor,
            IBlockchainClientFactory<AElfClient> blockchainClientFactory,
            IOptionsMonitor<ChainOptions> chainOptionsMonitor)
        {
            _userAppService = userAppService;
            _nftListingProvider = nftListingProvider;
            _logger = logger;
            _objectMapper = objectMapper;
            _compositeNFTProvider = compositeNFTProvider;
            _nftCollectionExtensionProvider = nftCollectionExtensionProvider;
            _statisticsOptionsMonitor = statisticsOptionsMonitor;
            _blockchainClientFactory = blockchainClientFactory;
            _chainOptionsMonitor = chainOptionsMonitor;


        }

        public async Task<PagedResultDto<NFTListingIndexDto>> GetNFTListingsAsync(GetNFTListingsInput input)
        {
            try
            {
                if (input.Symbol.IsNullOrEmpty() || !input.Symbol.MatchesNftSymbol())
                {
                    throw new UserFriendlyException("Symbol invalid..");
                }
                var getNftListingsDto = _objectMapper.Map<GetNFTListingsInput, GetNFTListingsDto>(input);
                var listingDto = await _nftListingProvider.GetNFTListingsAsync(getNftListingsDto);

                var listingOwner = listingDto.Items.Select(i => i?.Owner ?? "").ToList();
                var collectionCreator =
                    listingDto.Items.Select(i => i?.NftCollectionDto?.CreatorAddress ?? "").ToList();
                
                // query account info by account address
                var addresses = listingOwner
                    .Concat(collectionCreator)
                    .Where(s => !string.IsNullOrEmpty(s))
                    .Distinct().ToList();
                var accountDict = await _userAppService.GetAccountsAsync(addresses);
                var nftInfoIdList = new List<string>() {input.ChainId+"_"+input.Symbol};
                var compositeNFTInfoDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(nftInfoIdList);

                var res = listingDto.Items.Select(i =>
                {
                    _logger.LogInformation("GetNFTListingsAsync step1 listInfo:{A}",JsonConvert.SerializeObject(i));
                    i.WhitelistId = i.WhitelistId;
                    var item = _objectMapper.Map<IndexerNFTListingInfo, NFTListingIndexDto>(i);
                    item.Owner = accountDict.GetValueOrDefault(i.Owner, new AccountDto(i.Owner))?.WithChainIdAddress(item.ChainId);
                    item.PurchaseToken = _objectMapper.Map<IndexerTokenInfo, TokenDto>(i.PurchaseToken);
                    //item.NFTInfo = _objectMapper.Map<IndexerNFTInfo, NFTImmutableInfoDto>(i.NftInfo);
                    
                    item.Decimals = compositeNFTInfoDic[i.BusinessId].Decimals;
                    item.RealQuantity = FTHelper.GetIntegerDivision(i.RealQuantity, item.Decimals);
                    item.OriginQuantity = i.RealQuantity;   
                    if (item.NFTInfo == null) return item;
                    
                    item.NFTInfo.NftCollection =
                        _objectMapper.Map<IndexerNFTCollection, NFTCollectionIndexDto>(i.NftCollectionDto);
                    if (!i.NftCollectionDto?.CreatorAddress?.IsNullOrEmpty() ?? false)
                    {
                        item.NFTInfo.NftCollection.Creator = accountDict.GetValueOrDefault(
                            i.NftCollectionDto.CreatorAddress,
                            new AccountDto(i.NftCollectionDto.CreatorAddress))?
                            .WithChainIdAddress(i.NftCollectionDto.ChainId);
                    }
                    return item;
                }).ToList();

                return new PagedResultDto<NFTListingIndexDto>
                {
                    Items = res,
                    TotalCount = listingDto.TotalCount
                };
            }
            catch (Exception e)
            {
                _logger.LogError(e, "GetNFTListingsAsync ERROR");
                throw new UserFriendlyException("Internal error, please try again later.");
            }
        }

        public async Task<PagedResultDto<CollectedCollectionListingDto>> GetCollectedCollectionListingAsync(
            GetCollectedCollectionListingsInput input)
        {
            input.Address = FullAddressHelper.ToShortAddress(input.Address);
            
            var nftInfoIds = new List<string>();
            if (!input.SearchParam.IsNullOrEmpty() || !input.CollectionIdList.IsNullOrEmpty())
            {
                int skip = CommonConstant.IntZero;
                var compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                    input.SearchParam, skip, CommonConstant.IntOneThousand);
                nftInfoIds = compositeNFTDic?.Keys.ToList();
                while (nftInfoIds.Count >= CommonConstant.IntOneThousand)
                {
                    skip += CommonConstant.IntOneThousand;
                    compositeNFTDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(input.CollectionIdList,
                        input.SearchParam, skip, CommonConstant.IntOneThousand);
                    var infoIds = compositeNFTDic?.Keys.ToList();
                    if (infoIds.IsNullOrEmpty())
                    {
                        break;
                    }
                    nftInfoIds.AddRange(infoIds);
                }
                
                
                if (nftInfoIds.IsNullOrEmpty())
                {
                    return new PagedResultDto<CollectedCollectionListingDto>()
                    {
                        TotalCount = CommonConstant.IntZero,
                        Items = new List<CollectedCollectionListingDto>()
                    };
                }
            }

            var collectedNFTListings = await _nftListingProvider.GetCollectedNFTListingsAsync(input.SkipCount,
                input.MaxResultCount, input.Address, input.ChainList, nftInfoIds);

            if (collectedNFTListings == null || collectedNFTListings.TotalRecordCount == CommonConstant.IntZero)
            {
                return new PagedResultDto<CollectedCollectionListingDto>()
                {
                    TotalCount = CommonConstant.IntZero,
                    Items = new List<CollectedCollectionListingDto>()
                };
            }
            
            var nftInfoIdList = collectedNFTListings.IndexerNFTListingInfoList?.Select(item => item.BusinessId).ToList();

            var nftCollectionExtensionDic =
                await _nftCollectionExtensionProvider.GetNFTCollectionExtensionsAsync(nftInfoIdList
                    .Select(item => SymbolHelper.TransferNFTIdToCollectionId(item)).ToList());
            
            var compositeNFTInfoDic = await _compositeNFTProvider.QueryCompositeNFTInfoAsync(nftInfoIdList);
            
            var listingOwner = collectedNFTListings.IndexerNFTListingInfoList?.Select(i => i?.Owner ?? "").ToList();
            
            var addresses = listingOwner
                .Where(s => !string.IsNullOrEmpty(s))
                .Distinct().ToList();
            var accountDict = await _userAppService.GetAccountsAsync(addresses);

            var res = collectedNFTListings.IndexerNFTListingInfoList.Select(i =>
            {
                var item = _objectMapper.Map<IndexerNFTListingInfo, CollectedCollectionListingDto>(i);
                item.Owner = accountDict.GetValueOrDefault(i.Owner, new AccountDto(i.Owner))
                    ?.WithChainIdAddress(item.ChainId);
                item.PurchaseToken = _objectMapper.Map<IndexerTokenInfo, TokenDto>(i.PurchaseToken);
                if (compositeNFTInfoDic.ContainsKey(i.BusinessId) && compositeNFTInfoDic[i.BusinessId] != null)
                {
                    item.PreviewImage = compositeNFTInfoDic[i.BusinessId].PreviewImage;
                    item.NFTName = compositeNFTInfoDic[i.BusinessId].NFTName;
                    item.CollectionName = compositeNFTInfoDic[i.BusinessId].CollectionName;
                    item.Decimals = compositeNFTInfoDic[i.BusinessId].Decimals;
                    item.Prices = item.Prices;
                    item.NFTSymbol = compositeNFTInfoDic[i.BusinessId].Symbol;
                    item.NFTInfoId = compositeNFTInfoDic[i.BusinessId].NFTInfoId;
                    item.Price = item.Prices;
                    item.Quantity = FTHelper.GetIntegerDivision(i.RealQuantity, item.Decimals);
                    item.OriginQuantity = i.Quantity;
                }
                var collectionId = SymbolHelper.TransferNFTIdToCollectionId(i.BusinessId);
                if (nftCollectionExtensionDic.ContainsKey(collectionId) && nftCollectionExtensionDic[collectionId] != null)
                {
                    item.FloorPrice = nftCollectionExtensionDic[collectionId] .FloorPrice;
                    item.FloorPriceSymbol = nftCollectionExtensionDic[collectionId] .FloorPriceSymbol;
                    item.CollectionLogoImage =
                        FTHelper.BuildIpfsUrl(nftCollectionExtensionDic[collectionId].LogoImage);
                }

                return item;
            }).ToList();

            return new PagedResultDto<CollectedCollectionListingDto>
            {
                Items = res,
                TotalCount = collectedNFTListings.TotalRecordCount
            };
        }
        
        public async Task<ResultDto<string>> StatisticsUserListRecord(GetNFTListingsInput input)
        {
            try
            {
                var statisticsSwitch = _statisticsOptionsMonitor.CurrentValue.StatisticsSwitch;
                var sendTxSwitch = _statisticsOptionsMonitor.CurrentValue.SendTxSwitch;
                _logger.LogInformation("StatisticsUserListRecord Step0 statisticsSwitch:{A} sendTxSwitch:{B}", statisticsSwitch, sendTxSwitch);

                if (!statisticsSwitch)
                {
                    return new ResultDto<string>() {Success = true, Message = "statisticsSwitch is false"};
 
                }

                var maxQueryCount = 10;
                var queryCount = 0;
                var getNftListingsDto = _objectMapper.Map<GetNFTListingsInput, GetNFTListingsDto>(input);
                var allListing = new List<IndexerNFTListingInfo>();
                
                var skipCount = 0;
                var maxPageCount = 10000;
                var totalCount = 0l;
                
                //query list records
                while (queryCount < maxQueryCount)
                {
                    getNftListingsDto.SkipCount = skipCount;
                    getNftListingsDto.MaxResultCount = maxPageCount;
                    getNftListingsDto.BlockHeight = 0;
                    
                    var listingDto = await _nftListingProvider.GetAllNFTListingsByHeightAsync(getNftListingsDto);
                    totalCount = (totalCount==0) ? listingDto.TotalCount : totalCount;
                    var itemCount = listingDto.Items.Count;
                    allListing.AddRange(listingDto.Items);
                    if (itemCount < maxPageCount)
                    {
                        break;
                    }
                    _logger.LogInformation("StatisticsUserListRecord Step1 queryCount:{A} totalCount:{B} itemCount:{C}", queryCount, totalCount, itemCount);

                    skipCount += maxPageCount;
                    queryCount++;
                }

                _logger.LogInformation("StatisticsUserListRecord Step2 totalCount:{A} itemCount:{B}", totalCount, allListing.Count);

                //statistics user list records key:address+":"+collectionSymbol
                Dictionary<string, long> listDictionary = new Dictionary<string, long>();
                foreach (var list in allListing)
                {
                    var key = list.Owner + NFTSymbolBasicConstants.StatisticsKeySeparator +
                              TransferCollectionSymbol(list.Symbol);
                    if(listDictionary.TryGetValue(key, out var value))
                    {
                        listDictionary[key] = value + list.Quantity;
                    }
                    else
                    {
                        listDictionary.Add(key, list.Quantity);
                    }
                }
                _logger.LogInformation("StatisticsUserListRecord Step3 listDictionary size:{A}", listDictionary.Count);

                //send tx
                if (!sendTxSwitch)
                {
                    return new ResultDto<string>() {Success = true, Message = "sendTxSwitch is false"};
 
                }
                foreach (var key in listDictionary.Keys)
                {
                    listDictionary.TryGetValue(key, out var count);
                    _logger.LogInformation("StatisticsUserListRecord Step4 send tx prepare listDictionary address:{A} count:{B}", key, count);
                    var keySplitArr = key.Split(NFTSymbolBasicConstants.StatisticsKeySeparator);
                    await SendSetCollectionListTotalCountTxAsync(keySplitArr[0], keySplitArr[1], count, input.ChainId);
                    Thread.Sleep(1000);
                }
                return new ResultDto<string>() {Success = true, Message = "update address count " + listDictionary.Count};

            }
            catch (Exception e)
            {
                _logger.LogError(e, "StatisticsUserListRecord ERROR");
                return new ResultDto<string>() {Success = false, Message = e.Message};
            }
        }
        
        public static string TransferCollectionSymbol(string symbol)
        {
            if (string.IsNullOrEmpty(symbol))
                return symbol;

            var parts = symbol.Split(NFTSymbolBasicConstants.NFTSymbolSeparator);

            if (parts.Length == 1)
            {
                return parts[0];
            }

            if (parts.Length == 2)
            {
                var baseString = parts[0];
                if (int.TryParse(parts[1], out int number))
                {
                    return $"{baseString}{NFTSymbolBasicConstants.NFTSymbolSeparator}{NFTSymbolBasicConstants.CollectionSymbolSuffix}";
                }
            }

            return symbol;
        }

        public async Task SendSetCollectionListTotalCountTxAsync(string address, string symbol, long count, string chainId)
        {
            var setCollectionListTotalCountParam =
                new SetCollectionListTotalCountInput
                {
                    Symbol = symbol,
                    Address = Address.FromBase58(address),
                    Count = count
                };
            var client = _blockchainClientFactory.GetClient(chainId);
            var chainInfo = _chainOptionsMonitor.CurrentValue.ChainInfos[chainId];

            var setCollectionListTotalCountParamRaw =
                await GenerateRawTransaction(client, "SetCollectionListTotalCount", setCollectionListTotalCountParam,
                    chainInfo.ForestContractAddress, chainInfo.PrivateKey);
            
            var validateTokenInfoExistsResult = await client.SendTransactionAsync(new SendTransactionInput()
                { RawTransaction = setCollectionListTotalCountParamRaw });
            _logger.LogInformation("StatisticsUserListRecord Step5 send tx address:{A} count:{B} symbol:{C} txId:{D}", address, count, symbol, validateTokenInfoExistsResult.TransactionId);

        }
        
        private async Task<string> GenerateRawTransaction(AElfClient client, string methodName, IMessage param,
            string contractAddress, string privateKey)
        {
            return client.SignTransaction(privateKey, await client.GenerateTransactionAsync(
                    client.GetAddressFromPrivateKey(privateKey), contractAddress, methodName, param))
                .ToByteArray().ToHex();
        }
    }
    
    
}