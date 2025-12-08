using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Common;
using NFTMarketServer.NFT.Eto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Options;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;
using Newtonsoft.Json;
using NFTMarketServer.Tokens;
using Orleans.Runtime;


namespace NFTMarketServer.NFT
{
    [RemoteService(IsEnabled = false)]
    public class NFTDropAppService : NFTMarketServerAppService, INFTDropAppService
    {
        private readonly ILogger<NFTDropAppService> _logger;
        private readonly INESTRepository<NFTInfoIndex, string> _nftInfoIndexRepository;
        private readonly IDistributedEventBus _distributedEventBus;
        private readonly IObjectMapper _objectMapper;
        private readonly INFTDropExtensionProvider _dropExtensionProvider;
        private readonly INFTDropInfoProvider _dropInfoProvider;
        private readonly INFTCollectionExtensionProvider _dropCollectionExtensionProvider;
        private readonly IOptionsMonitor<RecommendedDropOptions> _optionsMonitor;
        private readonly ITokenAppService _tokenAppService;

        public NFTDropAppService(
            ILogger<NFTDropAppService> logger,
            IDistributedEventBus distributedEventBus,
            IObjectMapper objectMapper,
            INFTDropExtensionProvider dropExtensionProvider,
            INFTDropInfoProvider dropInfoProvider,
            INFTCollectionExtensionProvider dropCollectionExtensionProvider,
            IOptionsMonitor<RecommendedDropOptions> optionsMonitor,
            ITokenAppService tokenAppService)
        {
            _logger = logger;
            _distributedEventBus = distributedEventBus;
            _objectMapper = objectMapper;
            _dropExtensionProvider = dropExtensionProvider;
            _dropInfoProvider = dropInfoProvider;
            _dropCollectionExtensionProvider = dropCollectionExtensionProvider;
            _optionsMonitor = optionsMonitor;
            _tokenAppService = tokenAppService;
        }

        public async Task CreateNFTDropExtensionAsync(CreateNFTDropInput input)
        {
            
            _logger.LogInformation("CreateNFTInfoExtensionAsync , req: {req}", JsonConvert.SerializeObject(input));
            
            var ids = new List<string>
            {
                input.DropId
            };
            var dropExtensionMap = await _dropExtensionProvider.BatchGetNFTDropExtensionAsync(ids);
            if (!dropExtensionMap.IsNullOrEmpty())
            {
                throw new UserFriendlyException("drop already exist");
            }

            await _distributedEventBus.PublishAsync(
                _objectMapper.Map<CreateNFTDropInput, NFTDropExtraEto>(input));
        }

        public async Task<PagedResultDto<NFTDropIndexDto>> GetNFTDropListAsync(GetNFTDropListInput input)
        {
            _logger.LogInformation("GetNFTDropListAsync , req: {req}", JsonConvert.SerializeObject(input));
            var dropInfoList = await _dropInfoProvider.GetNFTDropInfoIndexListAsync(input);
            if (dropInfoList.TotalRecordCount == 0)
            {
                return new PagedResultDto<NFTDropIndexDto>
                {
                    TotalCount = 0
                };
            }

            var ids = dropInfoList.DropInfoIndexList.Select(i => i.DropId).ToList();
            var dropExtensionMap = await _dropExtensionProvider.BatchGetNFTDropExtensionAsync(ids);

            
            var dropList = new List<NFTDropIndexDto>();
            foreach (var dropInfo in dropInfoList.DropInfoIndexList)
            {
                var dropIndexDto = new NFTDropIndexDto
                {
                    DropId = dropInfo.DropId,
                    StartTime = TimeHelper.ToUtcMilliSeconds(dropInfo.StartTime),
                    ExpireTime = TimeHelper.ToUtcMilliSeconds(dropInfo.ExpireTime),
                    MintPrice = dropInfo.ClaimPrice.ToString()
                };
                
                var usdPrice =
                    await _tokenAppService.GetCurrentDollarPriceAsync(dropInfo.ClaimSymbol, dropInfo.ClaimPrice);
                dropIndexDto.MintPriceUsd = usdPrice.ToString();
                
                if (dropExtensionMap.ContainsKey(dropInfo.DropId))
                {
                    _objectMapper.Map(dropExtensionMap[dropInfo.DropId], dropIndexDto);
                }
                dropList.Add(dropIndexDto);
            }

            return new PagedResultDto<NFTDropIndexDto>
            {
                Items = dropList,
                TotalCount = dropInfoList.TotalRecordCount
            };
        }

        public async Task<List<RecommendedNFTDropIndexDto>> GetRecommendedNFTDropListAsync()
        {
            _logger.LogInformation("GetRecommendedNFTDropListAsync");
            var recommendedOptions = _optionsMonitor.CurrentValue;
            if (recommendedOptions.RecommendedDropIds.IsNullOrEmpty())
            {
                return new List<RecommendedNFTDropIndexDto>();
            }
            
            var dropExtensionMap = await _dropExtensionProvider.BatchGetNFTDropExtensionAsync(recommendedOptions.RecommendedDropIds);


            var result = new List<RecommendedNFTDropIndexDto>();

            foreach (var id in recommendedOptions.RecommendedDropIds)
            {
                var dto = new RecommendedNFTDropIndexDto
                {
                    DropId = id,
                };
               
                if (dropExtensionMap.ContainsKey(id))
                {
                    _objectMapper.Map(dropExtensionMap[id], dto);
                }
                
                var dropInfo = await _dropInfoProvider.GetNFTDropInfoIndexAsync(id);
                if (dropInfo != null && dropInfo.State != NFTDropState.Cancel)
                {
                    dto.MintPrice = dropInfo.ClaimPrice.ToString();
                    var usdPrice =
                        await _tokenAppService.GetCurrentDollarPriceAsync(dropInfo.ClaimSymbol, dropInfo.ClaimPrice);
                    dto.MintPriceUsd = usdPrice.ToString();
                    result.Add(dto);
                }
            }

            return result;
        }

        public async Task<NFTDropDetailDto> GetNFTDropDetailAsync(GetNFTDropDetailInput input)
        {
            _logger.LogInformation("GetNFTDropDetailAsync, req:{req}", JsonConvert.SerializeObject(input));
            var dropInfo = await _dropInfoProvider.GetNFTDropInfoIndexAsync(input.DropId);
            
            var dropDetailDto = _objectMapper.Map<NFTDropInfoIndex, NFTDropDetailDto>(dropInfo);
            
            if (dropInfo == null)
            {
                return dropDetailDto;
            }
            dropDetailDto.AddressClaimLimit = dropInfo.ClaimMax;
            
            var usdPrice =
                await _tokenAppService.GetCurrentDollarPriceAsync(dropInfo.ClaimSymbol, dropInfo.ClaimPrice);
            dropDetailDto.MintPriceUsd = usdPrice.ToString();
            dropDetailDto.MintPrice = dropInfo.ClaimPrice.ToString();
            
            var ids = new List<string>
            {
                input.DropId
            };
            
            var dropExtensionMap = await _dropExtensionProvider.BatchGetNFTDropExtensionAsync(ids);
            if (!dropExtensionMap.IsNullOrEmpty())
            {
                _logger.LogDebug("Fill dropExtension: {dropExtension}", JsonConvert.SerializeObject(dropExtensionMap));
                var extensionInfo = dropExtensionMap[input.DropId];
                _objectMapper.Map(extensionInfo, dropDetailDto);
            }

            var collectionInfo =
                await _dropCollectionExtensionProvider.GetNFTCollectionExtensionAsync(dropDetailDto.CollectionId);
            if (collectionInfo != null)
            {
                _logger.LogDebug("Fill collectionInfo: {collectionInfo}", JsonConvert.SerializeObject(collectionInfo));
                dropDetailDto.CollectionLogo = collectionInfo.LogoImage;
                dropDetailDto.CollectionName = collectionInfo.TokenName;
            }
            
            if (input.Address.IsNullOrEmpty())
            {
                return dropDetailDto;
            }
            
            var claimInfo = await _dropInfoProvider.GetNFTDropClaimIndexAsync(input.DropId, input.Address);
            if (claimInfo == null)
            {
                _logger.LogInformation("claim not exist");
                return dropDetailDto;
            }
            
            dropDetailDto.AddressClaimAmount = claimInfo.ClaimTotal;
            _logger.LogDebug("Fill claimInfo: {claimInfo}", JsonConvert.SerializeObject(claimInfo));

            return dropDetailDto;
        }
        
        public async Task<NFTDropQuotaDto> GetNFTDropQuotaAsync(GetNFTDropQuotaInput input)
        {
            _logger.LogInformation("GetNFTDropQuotaAsync, req:{req}", JsonConvert.SerializeObject(input));
            var nftDropQuotaDto = new NFTDropQuotaDto();
            var dropInfo = await _dropInfoProvider.GetNFTDropInfoIndexAsync(input.DropId);
            if (dropInfo == null)
            {
                _logger.LogInformation("drop not exist");
                return nftDropQuotaDto;
            }
            
            _objectMapper.Map(dropInfo, nftDropQuotaDto);
            nftDropQuotaDto.AddressClaimLimit = dropInfo.ClaimMax;
            
            var claimInfo = await _dropInfoProvider.GetNFTDropClaimIndexAsync(input.DropId, input.Address);
            if (claimInfo == null)
            {
                _logger.LogInformation("claim not exist");
                return nftDropQuotaDto;
            }
            
            nftDropQuotaDto.AddressClaimAmount = claimInfo.ClaimTotal;
           
            return nftDropQuotaDto;
        }
    }
}