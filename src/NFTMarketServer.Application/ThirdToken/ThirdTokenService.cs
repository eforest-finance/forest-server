using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NFTMarketServer.Grains.Grain.ApplicationHandler;
using NFTMarketServer.Grains.Grain.ThirdToken;
using NFTMarketServer.Helper;
using NFTMarketServer.ThirdToken.Etos;
using NFTMarketServer.ThirdToken.Index;
using NFTMarketServer.ThirdToken.Provider;
using NFTMarketServer.TreeGame;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.ThirdToken;

public class ThirdTokenService : IThirdTokenService, ISingletonDependency
{
    private readonly IThirdTokenProvider _thirdTokenProvider;
    private readonly ITokenVerifyProvider _tokenVerifyProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IOptionsMonitor<TreeGameOptions> _platformOptionsMonitor;
    private readonly ILogger<ThirdTokenService> _logger;

    public ThirdTokenService(IThirdTokenProvider thirdTokenProvider, IObjectMapper objectMapper,
        IClusterClient clusterClient, IDistributedEventBus distributedEventBus,
        IOptionsMonitor<TreeGameOptions> platformOptionsMonitor, ILogger<ThirdTokenService> logger,
        ITokenVerifyProvider tokenVerifyProvider)
    {
        _thirdTokenProvider = thirdTokenProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _platformOptionsMonitor = platformOptionsMonitor;
        _logger = logger;
        _tokenVerifyProvider = tokenVerifyProvider;
    }

    public async Task<MyThirdTokenResult> GetMyThirdTokenListAsync(GetMyThirdTokenInput input)
    {
        var tokenRelationList = await _thirdTokenProvider.GetTokenRelationListAsync(input.Address, input.AelfToken);
        var thirdChainDic = tokenRelationList
            .GroupBy(x => x.ThirdChain)
            .ToDictionary(g => g.Key, g => g.ToList());

        var thirdTokenList = await _thirdTokenProvider.GetThirdTokenListAsync(
            thirdChainDic.Values.SelectMany(x => x.Select(t => t.ThirdToken)).ToList(), thirdChainDic.Keys.ToList());

        var thirdTokenIdDic = thirdTokenList
            .ToDictionary(x => GuidHelper.UniqId(x.TokenName, x.Symbol, x.Chain, x.Address), x => x);

        var result = _objectMapper.Map<List<TokenRelationIndex>, List<MyThirdTokenDto>>(tokenRelationList);

        result.ForEach(dto =>
        {
            if (!thirdTokenIdDic.TryGetValue(
                    GuidHelper.UniqId(dto.ThirdTokenName, dto.ThirdSymbol, dto.ThirdChain, dto.Address),
                    out var thirdToken))
            {
                return;
            }

            dto.ThirdSymbol = thirdToken.Symbol;
            dto.ThirdTokenImage = thirdToken.TokenImage;
            dto.ThirdContractAddress = thirdToken.TokenContractAddress;
            dto.ThirdTotalSupply = thirdToken.TotalSupply;
        });

        return new MyThirdTokenResult()
        {
            TotalCount = result.Count,
            Items = result
        };
    }

    public async Task<ThirdTokenPrepareBindingDto> ThirdTokenPrepareBindingAsync(ThirdTokenPrepareBindingInput input)
    {
        var requestHash = BuildRequestHash(string.Concat(input.Address, input.AelfToken, input.AelfChain,
            input.ThirdTokens.TokenName, input.ThirdTokens.Symbol, input.ThirdTokens.TokenImage,
            input.ThirdTokens.TotalSupply.ToString(), input.ThirdTokens.Owner, input.ThirdTokens.ThirdChain,
            input.ThirdTokens.ContractAddress));
        if (requestHash != input.Signature)
        {
            throw new UserFriendlyException("invalid request");
        }

        var id = GuidHelper.UniqId(input.Address, input.AelfChain, input.AelfToken, input.ThirdTokens.ThirdChain,
            input.ThirdTokens.TokenName);
        var tokenRelationGrain = _clusterClient.GetGrain<ITokenRelationGrain>(id.ToString());
        var tokenRelationGrainDto = _objectMapper.Map<ThirdTokenPrepareBindingInput, TokenRelationGrainDto>(input);
        var tokenRelationRecord = await tokenRelationGrain.CreateTokenRelationAsync(tokenRelationGrainDto);

        var thirdTokenId = GuidHelper.UniqId(input.Address, input.ThirdTokens.ThirdChain, input.ThirdTokens.TokenName,
            input.ThirdTokens.Symbol);
        var thirdTokenGrain = _clusterClient.GetGrain<IThirdTokenGrain>(thirdTokenId.ToString());
        var thirdTokenGrainDto = _objectMapper.Map<ThirdTokenPrepareBindingInput, ThirdTokenGrainDto>(input);
        var thirdTokenRecord = await thirdTokenGrain.CreateThirdTokenAsync(thirdTokenGrainDto);

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<TokenRelationGrainDto, TokenRelationEto>(tokenRelationRecord.Data));
        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<ThirdTokenGrainDto, ThirdTokenEto>(thirdTokenRecord.Data));

        return new ThirdTokenPrepareBindingDto
        {
            BindingId = tokenRelationRecord.Data.Id,
            ThirdTokenId = thirdTokenRecord.Data.Id,
            ChainName = thirdTokenRecord.Data.Chain
        };
    }

    public async Task<string> ThirdTokenBindingAsync(ThirdTokenBindingInput input)
    {
        var associatedTokenAccount = input.AssociatedTokenAccount;
        var deployedTokenContractAddress = input.TokenContractAddress;
        var mintToAddress = input.MintToAddress;
        var requestHash = BuildRequestHash(string.Concat(input.BindingId, input.ThirdTokenId,
            deployedTokenContractAddress, associatedTokenAccount, mintToAddress));
        if (requestHash != input.Signature)
        {
            throw new UserFriendlyException("invalid request.");
        }

        var thirdTokenGrain = _clusterClient.GetGrain<IThirdTokenGrain>(input.ThirdTokenId);
        var thirdToken = await thirdTokenGrain.GetThirdTokenAsync();
        if (thirdToken.Success == false)
        {
            _logger.LogWarning("not found third token");
            throw new UserFriendlyException("invalid token");
        }

        await AutoVerifyTokenAsync(thirdToken.Data);

        var thirdTokenExist = await _thirdTokenProvider.CheckThirdTokenExistAsync(thirdToken.Data.Chain,
            thirdToken.Data.TokenName, thirdToken.Data.Symbol, deployedTokenContractAddress, associatedTokenAccount);
        if (!thirdTokenExist)
        {
            _logger.LogWarning("not found in contract. chain: {chain}, token: {token},symbol: {symbol}",
                thirdToken.Data.Chain, thirdToken.Data.TokenName, thirdToken.Data.Symbol);
            throw new UserFriendlyException("invalid token");
        }

        var tokenRelationGrain = _clusterClient.GetGrain<ITokenRelationGrain>(input.BindingId);
        var tokenRelationRecord = await tokenRelationGrain.BoundAsync();
        var thirdTokenRecord =
            await thirdTokenGrain.FinishedAsync(deployedTokenContractAddress, associatedTokenAccount);
        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<TokenRelationGrainDto, TokenRelationEto>(tokenRelationRecord.Data));
        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<ThirdTokenGrainDto, ThirdTokenEto>(thirdTokenRecord.Data));
        return "";
    }

    private async Task AutoVerifyTokenAsync(ThirdTokenGrainDto dto)
    {
        await _tokenVerifyProvider.AutoVerifyTokenAsync(dto);
    }

    private string BuildRequestHash(string request)
    {
        var hashVerifyKey = _platformOptionsMonitor.CurrentValue.HashVerifyKey ?? TreeGameConstants.HashVerifyKey;
        var requestHash = HashHelper.ComputeFrom(string.Concat(request, hashVerifyKey));
        return requestHash.ToHex();
    }
}