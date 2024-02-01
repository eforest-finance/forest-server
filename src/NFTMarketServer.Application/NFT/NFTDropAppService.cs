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

        public NFTDropAppService(
            ILogger<NFTDropAppService> logger,
            IDistributedEventBus distributedEventBus,
            IObjectMapper objectMapper,
            INFTDropExtensionProvider dropExtensionProvider,
            INFTDropInfoProvider dropInfoProvider,
            INFTCollectionExtensionProvider dropCollectionExtensionProvider,
            IOptionsMonitor<RecommendedDropOptions> optionsMonitor)
        {
            _logger = logger;
            _distributedEventBus = distributedEventBus;
            _objectMapper = objectMapper;
            _dropExtensionProvider = dropExtensionProvider;
            _dropInfoProvider = dropInfoProvider;
            _dropCollectionExtensionProvider = dropCollectionExtensionProvider;
            _optionsMonitor = optionsMonitor;
        }

        public async Task CreateNFTDropExtensionAsync(CreateNFTDropInput input)
        {
            
            _logger.LogInformation("CreateNFTInfoExtensionAsync , req: {req}", JsonConvert.SerializeObject(input));
            // var extension = new NftInfoExtensionGrainDto()
            // {
            //     Id = id,
            //     ChainId = input.ChainId,
            //     Description = input.Description,
            //     NFTSymbol = input.Symbol,
            //     TransactionId = input.TransactionId,
            //     ExternalLink = input.ExternalLink,
            //     PreviewImage = input.PreviewImage,
            //     File = input.File,
            //     FileExtension = fileExtension,
            //     CoverImageUrl = input.CoverImageUrl
            // };
            // var userGrain = _clusterClient.GetGrain<INftInfoExtensionGrain>(extension.Id);
            // var result = await userGrain.CreateNftInfoExtensionAsync(extension);
            // if (!result.Success)
            // {
            //     _logger.LogError("Create NftInfoExtension fail, NftInfoExtension id: {id}.", extension.Id);
            //     return;
            // }

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

            var dropList = dropInfoList.DropInfoIndexList.Select(i =>
            {
                if (dropExtensionMap.ContainsKey(i.DropId))
                {
                    var dropIndexDto =
                        _objectMapper.Map<NFTDropExtensionIndex, NFTDropIndexDto>(dropExtensionMap[i.DropId]);

                    return dropIndexDto;
                }
                else
                {
                    return new NFTDropIndexDto
                    {
                        DropId = i.DropId,
                        StartTime = i.StartTime,
                        ExpireTime = i.ExpireTime,
                        ClaimPrice = i.ClaimPrice
                    };
                }
            }).ToList();

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
            
            var res = recommendedOptions.RecommendedDropIds.Select(i =>
            {
                if (dropExtensionMap.ContainsKey(i))
                {
                    var dropIndexDto =
                        _objectMapper.Map<NFTDropExtensionIndex, RecommendedNFTDropIndexDto>(dropExtensionMap[i]);

                    return dropIndexDto;
                }
                else
                {
                    return new RecommendedNFTDropIndexDto
                    {
                        DropId = i,
                    };
                }
            }).ToList();


            return res;
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

            var ids = new List<string>
            {
                input.DropId
            };
            
            var dropExtensionMap = await _dropExtensionProvider.BatchGetNFTDropExtensionAsync(ids);
            if (!dropExtensionMap.IsNullOrEmpty())
            {
                _logger.Debug("Fill dropExtension: {dropExtension}", JsonConvert.SerializeObject(dropExtensionMap));
                var extensionInfo = dropExtensionMap[input.DropId];
                _objectMapper.Map(extensionInfo, dropDetailDto);
            }

            var collectionInfo =
                await _dropCollectionExtensionProvider.GetNFTCollectionExtensionAsync(dropDetailDto.CollectionId);
            if (collectionInfo != null)
            {
                _logger.Debug("Fill collectionInfo: {collectionInfo}", JsonConvert.SerializeObject(collectionInfo));
                dropDetailDto.CollectionLogo = collectionInfo.LogoImage;
                dropDetailDto.CollectionName = collectionInfo.TokenName;
            }
            
            if (input.Address.IsNullOrEmpty())
            {
                return dropDetailDto;
            }
            
            var claimInfo = await _dropInfoProvider.GetNFTDropClaimIndexAsync(input.DropId, input.Address);
            dropDetailDto.AddressClaimAmount = claimInfo.ClaimAmount;
            _logger.Debug("Fill claimInfo: {claimInfo}", JsonConvert.SerializeObject(claimInfo));

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
            
            var claimInfo = await _dropInfoProvider.GetNFTDropClaimIndexAsync(input.DropId, input.Address);
            nftDropQuotaDto.AddressClaimAmount = claimInfo.ClaimAmount;
           
            return nftDropQuotaDto;
        }
    }
}