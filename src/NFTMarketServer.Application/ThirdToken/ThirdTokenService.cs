using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NFTMarketServer.Grains.Grain.ThirdToken;
using NFTMarketServer.Helper;
using NFTMarketServer.ThirdToken.Etos;
using NFTMarketServer.ThirdToken.Index;
using NFTMarketServer.ThirdToken.Provider;
using Orleans;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.ThirdToken;

public class ThirdTokenService : IThirdTokenService, ISingletonDependency
{
    private readonly IThirdTokenProvider _thirdTokenProvider;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;

    public ThirdTokenService(IThirdTokenProvider thirdTokenProvider, IObjectMapper objectMapper,
        IClusterClient clusterClient, IDistributedEventBus distributedEventBus)
    {
        _thirdTokenProvider = thirdTokenProvider;
        _objectMapper = objectMapper;
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
    }

    public async Task<List<MyThirdTokenDto>> GetMyThirdTokenListAsync(GetMyThirdTokenInput input)
    {
        var tokenRelationList = await _thirdTokenProvider.GetTokenRelationListAsync(input.Address, input.AelfToken);
        var thirdChainDic = tokenRelationList
            .GroupBy(x => x.ThirdChain)
            .ToDictionary(g => g.Key, g => g.ToList());

        var thirdTokenList = await _thirdTokenProvider.GetThirdTokenListAsync(
            thirdChainDic.Values.SelectMany(x => x.Select(t => t.ThirdToken)).ToList(), thirdChainDic.Keys.ToList());

        var thirdTokenIdDic = thirdTokenList
            .ToDictionary(x => IdGenerator.GenerateId(x.TokenName, x.Chain), x => x);

        var result = _objectMapper.Map<List<TokenRelationIndex>, List<MyThirdTokenDto>>(tokenRelationList);

        result.ForEach(dto =>
        {
            if (!thirdTokenIdDic.TryGetValue(IdGenerator.GenerateId(dto.ThirdTokenName, dto.ThirdChain),
                    out var thirdToken))
            {
                return;
            }

            dto.ThirdSymbol = thirdToken.Symbol;
            dto.ThirdTokenImage = thirdToken.TokenImage;
            dto.ThirdContractAddress = thirdToken.ContractAddress;
            dto.ThirdTotalSupply = thirdToken.TotalSupply;
        });

        return result;
    }

    public async Task<ThirdTokenPrepareBindingDto> ThirdTokenPrepareBindingAsync(ThirdTokenPrepareBindingInput input)
    {
        var id = GuidHelper.UniqId(input.Address, input.AelfChain, input.AelfToken, input.ThirdTokens.ThirdChain,
            input.ThirdTokens.TokenName);
        var tokenRelationGrain = _clusterClient.GetGrain<ITokenRelationGrain>(id.ToString());
        var tokenRelationGrainDto = _objectMapper.Map<ThirdTokenPrepareBindingInput, TokenRelationGrainDto>(input);
        var tokenRelationRecord = await tokenRelationGrain.CreateTokenRelationAsync(tokenRelationGrainDto);

        var thirdTokenId = GuidHelper.UniqId(input.Address);
        var thirdTokenGrain = _clusterClient.GetGrain<IThirdTokenGrain>(thirdTokenId.ToString());
        var thirdTokenGrainDto = _objectMapper.Map<ThirdTokenPrepareBindingInput, ThirdTokenGrainDto>(input);
        var thirdTokenRecord = await thirdTokenGrain.CreateThirdTokenAsync(thirdTokenGrainDto);

        return new ThirdTokenPrepareBindingDto
        {
            BindingId = tokenRelationRecord.Data.Id,
            ThirdTokenId = thirdTokenRecord.Data.Id
        };
    }

    public async Task ThirdTokenBindingAsync(ThirdTokenBindingInput input)
    {
        var tokenRelationGrain = _clusterClient.GetGrain<ITokenRelationGrain>(input.BindingId);
        var tokenRelationRecord = await tokenRelationGrain.BoundAsync();
        var thirdTokenGrain = _clusterClient.GetGrain<IThirdTokenGrain>(input.ThirdTokenId);
        var thirdTokenRecord = await thirdTokenGrain.FinishedAsync();

        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<TokenRelationGrainDto, TokenRelationEto>(tokenRelationRecord.Data));
        await _distributedEventBus.PublishAsync(
            _objectMapper.Map<ThirdTokenGrainDto, ThirdTokenEto>(thirdTokenRecord.Data));
    }
}