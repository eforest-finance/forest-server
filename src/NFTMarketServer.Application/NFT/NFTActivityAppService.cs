using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.Helper;
using NFTMarketServer.NFT.Dto;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.Tokens;
using NFTMarketServer.Users;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.NFT;

[RemoteService(IsEnabled = false)]
public class NFTActivityAppService : NFTMarketServerAppService, INFTActivityAppService
{
    private readonly IUserAppService _userAppService;
    private readonly INFTActivityProvider _nftActivityProvider;
    private readonly IObjectMapper _objectMapper;

    public NFTActivityAppService(
        IUserAppService userAppService,
        INFTActivityProvider nftActivityProvider,
        IObjectMapper objectMapper)
    {
        _userAppService = userAppService;
        _nftActivityProvider = nftActivityProvider;
        _objectMapper = objectMapper;
    }

    public async Task<PagedResultDto<NFTActivityDto>> GetListAsync(GetActivitiesInput input)
    {
        var NFTActivityIndex = await _nftActivityProvider.GetNFTActivityListAsync(input.NFTInfoId, input.Types,
            input.TimestampMin,
            input.TimestampMax, input.SkipCount, input.MaxResultCount);
        var list = NFTActivityIndex?.IndexerNftActivity;
        if (list.IsNullOrEmpty())
        {
            return new PagedResultDto<NFTActivityDto>
            {
                Items = new List<NFTActivityDto>(),
                TotalCount = 0
            };
        }

        var addresses = new List<string>();
        foreach (var info in list)
        {
            if (!info.From.IsNullOrWhiteSpace())
            {
                addresses.Add(info.From);
            }

            if (!info.To.IsNullOrWhiteSpace())
            {
                addresses.Add(info.To);
            }
        }

        var accounts = await _userAppService.GetAccountsAsync(addresses);
        var result = list.Select(o => Map(o, accounts)).ToList();
        var totalCount = NFTActivityIndex.TotalRecordCount;
        return new PagedResultDto<NFTActivityDto>
        {
            Items = result,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResultDto<NFTActivityDto>> GetCollectionActivityListAsync(
        GetCollectionActivityListInput input)
    {
        var nftActivityIndex = await _nftActivityProvider.GetCollectionActivityListAsync(input.CollectionId,
            input.BizIdList, input.Types, input.SkipCount, input.MaxResultCount);
        var list = nftActivityIndex?.IndexerNftActivity;
        if (list.IsNullOrEmpty())
        {
            return new PagedResultDto<NFTActivityDto>
            {
                Items = new List<NFTActivityDto>(),
                TotalCount = 0
            };
        }

        var addresses = new List<string>();
        foreach (var info in list)
        {
            if (!info.From.IsNullOrWhiteSpace())
            {
                addresses.Add(info.From);
            }

            if (!info.To.IsNullOrWhiteSpace())
            {
                addresses.Add(info.To);
            }
        }

        var accounts = await _userAppService.GetAccountsAsync(addresses);
        var result = list.Select(o => Map(o, accounts)).ToList();
        var totalCount = nftActivityIndex.TotalRecordCount;
        return new PagedResultDto<NFTActivityDto>
        {
            Items = result,
            TotalCount = totalCount
        };
    }

    public async Task<PagedResultDto<CollectedCollectionActivitiesDto>> GetCollectedCollectionActivitiesAsync(
        GetCollectedCollectionActivitiesInput input, List<string> nftInfoIds)
    {
        var nftActivityIndexTuple = await _nftActivityProvider.GetCollectedCollectionActivitiesAsync(input, nftInfoIds);
        var list = nftActivityIndexTuple?.Item2;
        if (list.IsNullOrEmpty())
        {
            return new PagedResultDto<CollectedCollectionActivitiesDto>
            {
                Items = new List<CollectedCollectionActivitiesDto>(),
                TotalCount = 0
            };
        }

        var addresses = new List<string>();
        foreach (var info in list)
        {
            if (!info.From.IsNullOrWhiteSpace())
            {
                addresses.Add(info.From);
            }

            if (!info.To.IsNullOrWhiteSpace())
            {
                addresses.Add(info.To);
            }
        }

        var accounts = await _userAppService.GetAccountsAsync(addresses);
        var result = nftActivityIndexTuple.Item2.ToList().Select(item=>Map(item,accounts)).ToList();
        var totalCount = nftActivityIndexTuple.Item1;
        return new PagedResultDto<CollectedCollectionActivitiesDto>
        {
            Items = result,
            TotalCount = totalCount
        };
    }

    public async Task<Tuple<long, List<NFTActivityIndex>>> GetCollectedActivityListAsync(GetCollectedActivityListDto dto)
    {
        return await _nftActivityProvider.GetCollectedActivityListAsync(dto);
    }

    public async Task<Tuple<long, List<NFTActivityIndex>>> GetActivityByIdListAsync(List<string> idList)
    {
        return await _nftActivityProvider.GetActivityByIdListAsync(idList);
    }

    private CollectedCollectionActivitiesDto Map(NFTActivityIndex index, Dictionary<string, AccountDto> accounts)
    {
        var activityDto = _objectMapper.Map<NFTActivityIndex, CollectedCollectionActivitiesDto>(index);
        if (index.PriceTokenInfo != null)
        {
            activityDto.PriceToken = new TokenDto
            {
                Id = index.PriceTokenInfo.Id,
                ChainId = index.PriceTokenInfo.ChainId,
                Symbol = index.PriceTokenInfo.Symbol,
                Decimals = index.PriceTokenInfo.Decimals
            };
        }
        if (!index.From.IsNullOrWhiteSpace() && accounts.TryGetValue(index.From, out var account))
        {
            activityDto.From = account;
        }
        else
        {
            activityDto.From = new AccountDto(FullAddressHelper.ToShortAddress(index.From))
            {
                Name = index.From
            };
        }

        if (!index.To.IsNullOrWhiteSpace() && accounts.TryGetValue(index.To, out var account1))
        {
            activityDto.To = account1;
        }
        else
        {
            activityDto.To = new AccountDto(FullAddressHelper.ToShortAddress(index.To))
            {
                Name = index.To
            };
        }

        return activityDto;
    }

    private NFTActivityDto Map(NFTActivityItem index, Dictionary<string, AccountDto> accounts)
    {
        var activityDto = _objectMapper.Map<NFTActivityItem, NFTActivityDto>(index);
        if (index.PriceTokenInfo != null)
        {
            activityDto.PriceToken = new TokenDto
            {
                Id = index.PriceTokenInfo.Id,
                ChainId = index.PriceTokenInfo.ChainId,
                Symbol = index.PriceTokenInfo.Symbol,
                Decimals = index.PriceTokenInfo.Decimals
            };
        }
        if (!index.From.IsNullOrWhiteSpace() && accounts.TryGetValue(index.From, out var account))
        {
            activityDto.From = account;
        }
        else
        {
            activityDto.From = new AccountDto(FullAddressHelper.ToShortAddress(index.From))
            {
                Name = index.From
            };
        }

        if (!index.To.IsNullOrWhiteSpace() && accounts.TryGetValue(index.To, out var account1))
        {
            activityDto.To = account1;
        }
        else
        {
            activityDto.To = new AccountDto(FullAddressHelper.ToShortAddress(index.To))
            {
                Name = index.To
            };
        }

        return activityDto;
    }
}