using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Microsoft.IdentityModel.Tokens;
using NFTMarketServer.Common;
using NFTMarketServer.NFT;
using NFTMarketServer.NFT.Index;
using NFTMarketServer.NFT.Provider;
using NFTMarketServer.SymbolMarketToken.Index;
using NFTMarketServer.SymbolMarketToken.Provider;
using NFTMarketServer.Users.Provider;
using Volo.Abp.Application.Dtos;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.SymbolMarketToken;

public class SymbolMarketTokenAppService : ISymbolMarketTokenAppService, ISingletonDependency
{
    private const string TokenActionSupplyMaxed = "Supply Maxed";
    private const string TokenActionIssue = "Issue";
    private const string TokenActionNotIssuer = "Not Issuer";
    private readonly ISymbolMarketTokenProvider _symbolMarketTokenProvider;
    private readonly IUserInformationProvider _userInformationProvider;
    private readonly IObjectMapper _objectMapper;
    private ISymbolMarketTokenAppService _symbolMarketTokenAppServiceImplementation;
    private readonly INFTInfoExtensionProvider _nftInfoExtensionProvider;


    public SymbolMarketTokenAppService(ISymbolMarketTokenProvider symbolMarketTokenProvider,
        IUserInformationProvider userInformationProvider, IObjectMapper objectMapper,
        INFTInfoExtensionProvider nftInfoExtensionProvider)
    {
        _symbolMarketTokenProvider = symbolMarketTokenProvider;
        _userInformationProvider = userInformationProvider;
        _objectMapper = objectMapper;
        _nftInfoExtensionProvider = nftInfoExtensionProvider;
    }

    public async Task<PagedResultDto<SymbolMarketTokenDto>> GetSymbolMarketTokensAsync(GetSymbolMarketTokenInput input)
    {
        var allAddress = input?.AddressList;
        if (allAddress.IsNullOrEmpty())
        {
            return new PagedResultDto<SymbolMarketTokenDto>
            {
                Items = new List<SymbolMarketTokenDto>(),
                TotalCount = 0
            };
        }
        var innerAddressList = input?.AddressList
            .Select(address =>
            {
                if (address.IndexOf(ChainIdHelper.UNDERLINE) > -1)
                {
                    var parts = address.Split(ChainIdHelper.UNDERLINE);
                    return parts.Length > 1 ? parts[1] : address;
                }
                return address;
            })
            .Distinct()
            .ToList();
        
        var fullAddressListMap = input?.AddressList.Distinct().ToList()
            .Select(address =>
            {
                var parts = address.Split(ChainIdHelper.UNDERLINE);
                if (parts.Length < 2)
                {
                    return new KeyValuePair<string, string>(address, "");
                }
                var key = parts.Length == 3 ? parts[1] : address;
                var value = parts.Last();
                return new KeyValuePair<string, string>(key, value);
            })
            .GroupBy(pair => pair.Key)
            .Select(group =>
            {
                var pair = group.First();
                if (group.Count() > 1)
                {
                    pair = new KeyValuePair<string, string>(pair.Key, "");
                }
                return pair;
            })
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var result = await _symbolMarketTokenProvider.GetSymbolMarketTokenAsync(innerAddressList, input.SkipCount,
            input.MaxResultCount
        );

        if (result == null)
        {
            return new PagedResultDto<SymbolMarketTokenDto>
            {
                Items = new List<SymbolMarketTokenDto>(),
                TotalCount = 0
            };
        }

        var nftInfoExtensionIndexListDic =
            await _nftInfoExtensionProvider.GetNFTInfoExtensionsAsync(result.IndexerSymbolMarketTokenList
                ?.Select(token =>  IdGenerateHelper.GetNftExtensionId((int)token.IssueChainId,token.Symbol)).ToList());

        var items = result.IndexerSymbolMarketTokenList
            .Select(item => Map(item, innerAddressList, nftInfoExtensionIndexListDic, fullAddressListMap)).ToList();
        var totalCount = result.TotalRecordCount;

        return new PagedResultDto<SymbolMarketTokenDto>
        {
            Items = items,
            TotalCount = totalCount
        };
    }

    public async Task<SymbolMarketTokenIssuerDto> GetSymbolMarketTokenIssuerAsync(GetSymbolMarketTokenIssuerInput input)
    {
        var result = await _symbolMarketTokenProvider.GetSymbolMarketTokenIssuerAsync(input.IssueChainId,
            input.TokenSymbol
        );

        return new SymbolMarketTokenIssuerDto()
        {
            Issuer = result == null ? "" : result.SymbolMarketTokenIssuer
        };
    }


    private SymbolMarketTokenDto Map(IndexerSymbolMarketToken index, List<string> inputAddress,
        Dictionary<string, NFTInfoExtensionIndex> nftInfoExtensionIndexListDic,
        Dictionary<string,string> fullAddressListMap)
    {
        if (index == null) return null;
        var symbolMarketTokenDto = _objectMapper.Map<IndexerSymbolMarketToken, SymbolMarketTokenDto>(index);

        var nftInfoExtensionIndexId =
            IdGenerateHelper.GetNftExtensionId((int)index.IssueChainId, index.Symbol);
        if (nftInfoExtensionIndexListDic != null && nftInfoExtensionIndexListDic.ContainsKey(nftInfoExtensionIndexId) && nftInfoExtensionIndexListDic[nftInfoExtensionIndexId]!=null)
        {
            symbolMarketTokenDto.TokenImage = nftInfoExtensionIndexListDic[nftInfoExtensionIndexId].PreviewImage;
        }

        if (index.Issued == index.TotalSupply)
        {
            symbolMarketTokenDto.TokenAction = TokenActionSupplyMaxed;
        }
        else if (CheckTokenActionIssue(index, inputAddress, fullAddressListMap))
        {
            symbolMarketTokenDto.TokenAction = TokenActionIssue;
        }
        else
        {
            symbolMarketTokenDto.TokenAction = TokenActionNotIssuer;
        }

        return symbolMarketTokenDto;
    }

    private bool CheckTokenActionIssue(IndexerSymbolMarketToken index, List<string> inputAddress,
        Dictionary<string,string> fullAddressListMap)
    {
        var intersectList = index.IssueManagerList.Intersect(inputAddress).ToList();
        if (intersectList.IsNullOrEmpty()) return false;
        if (fullAddressListMap == null)
        {
            return !intersectList.IsNullOrEmpty();
        }

        for (int i = 0; i < intersectList.Count; i++)
        {
            if (!fullAddressListMap.ContainsKey(intersectList[i])) continue;
            if (fullAddressListMap[intersectList[i]].IsNullOrEmpty()) return true;
            if (fullAddressListMap[intersectList[i]]
                .Equals(ChainHelper.ConvertChainIdToBase58((int)index.IssueChainId))) return true;
        }
        
        return false;
    }
    
    public async Task<SymbolMarketTokenExistDto> GetSymbolMarketTokenExistAsync(GetSymbolMarketTokenExistInput input)
    {
        var result = await _symbolMarketTokenProvider.GetSymbolMarketTokenExistAsync(input.IssueChainId,
            input.TokenSymbol
        );

        if (result == null)
        {
            return new SymbolMarketTokenExistDto()
            {
                Exist = false
            };
        }

        if (!result.Symbol.IsNullOrEmpty())
        {
            return new SymbolMarketTokenExistDto()
            {
                Exist = true
            };
        }
        return new SymbolMarketTokenExistDto()
        {
            Exist = false
        };
    }
}