using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.Indexing.Elasticsearch;
using Microsoft.Extensions.Options;
using Nest;
using NFTMarketServer.Contracts.HandleException;
using NFTMarketServer.Options;
using NFTMarketServer.ThirdToken.Index;
using NFTMarketServer.ThirdToken.Strategy;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.ThirdToken.Provider;

public interface IThirdTokenProvider
{
    Task<List<ThirdTokenIndex>> GetThirdTokenListAsync(List<string> thirdToken, List<string> thirdChain);
    Task<List<TokenRelationIndex>> GetTokenRelationListAsync(string address, string aelfToken);
    Task<ThirdTokenInfo> GetThirdTokenInfoAsync(string chainName);

    Task<ThirdTokenExistDto> CheckThirdTokenExistAsync(string chainName, string tokenName, string tokenSymbol, string deployedAddress,
        string associatedTokenAccount);
}

public class ThirdTokenProvider : IThirdTokenProvider, ISingletonDependency
{
    private readonly INESTRepository<ThirdTokenIndex, string> _repository;
    private readonly INESTRepository<TokenRelationIndex, string> _tokenRelationRepository;
    private readonly IOptionsMonitor<ThirdTokenInfosOptions> _thirdTokenInfosOptionsMonitor;
    private readonly ThirdTokenStrategyFactory _factory;

    public ThirdTokenProvider(INESTRepository<TokenRelationIndex, string> tokenRelationRepository,
        INESTRepository<ThirdTokenIndex, string> repository,
        IOptionsMonitor<ThirdTokenInfosOptions> thirdTokenInfosOptionsMonitor, ThirdTokenStrategyFactory factory)
    {
        _tokenRelationRepository = tokenRelationRepository;
        _repository = repository;
        _thirdTokenInfosOptionsMonitor = thirdTokenInfosOptionsMonitor;
        _factory = factory;
    }


    public async Task<List<ThirdTokenIndex>> GetThirdTokenListAsync(List<string> thirdToken, List<string> thirdChain)
    {
        return await GetAllThirdTokenListAsync(thirdToken, thirdChain);
    }

    public async Task<List<TokenRelationIndex>> GetTokenRelationListAsync(string address, string aelfToken)
    {
        return await GetAllTokenRelationListAsync(address, aelfToken);
    }

    public async Task<ThirdTokenInfo> GetThirdTokenInfoAsync(string chainName)
    {
        var thirdTokenInfoDic = _thirdTokenInfosOptionsMonitor.CurrentValue.Chains
            .ToDictionary(x => x.ChainName, x => x);
        return thirdTokenInfoDic.TryGetValue(chainName, out var info) ? info : null;
    }

    [ExceptionHandler(typeof(Exception),
        Message = "ThirdTokenProvider.CheckThirdTokenExistAsync",
        TargetType = typeof(ExceptionHandlingService),
        MethodName = nameof(ExceptionHandlingService.HandleExceptionBoolRetrun),
        LogTargets = new[] { "chainName", "tokenName", "tokenSymbol" }
    )]
    public async Task<ThirdTokenExistDto> CheckThirdTokenExistAsync(string chainName, string tokenName,
        string tokenSymbol, string deployedAddress, string associatedTokenAccount)
    {
        var thirdTokenInfo = await GetThirdTokenInfoAsync(chainName);
        var thirdTokenStrategy = _factory.GetStrategy(thirdTokenInfo.Type);
        var abi = _thirdTokenInfosOptionsMonitor.CurrentValue.Abi;
        return await thirdTokenStrategy.CheckThirdTokenExistAsync(tokenName, tokenSymbol, deployedAddress,
            associatedTokenAccount, thirdTokenInfo, abi);
    }

    private async Task<List<TokenRelationIndex>> GetAllTokenRelationListAsync(string address, string aelfToken)
    {
        var res = new List<TokenRelationIndex>();
        var skipCount = 0;
        var maxResultCount = 5000;
        List<TokenRelationIndex> list;
        do
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<TokenRelationIndex>, QueryContainer>>();

            if (!string.IsNullOrEmpty(address))
            {
                mustQuery.Add(q => q.Term(i => i.Field(f => f.Address).Value(address)));
            }
            mustQuery.Add(q => q.Term(i => i.Field(f => f.AelfToken).Value(aelfToken)));
            mustQuery.Add(q => q.Term(i => i.Field(f => f.RelationStatus).Value(RelationStatus.Bound)));

            QueryContainer Filter(QueryContainerDescriptor<TokenRelationIndex> f) => f.Bool(b => b.Must(mustQuery));
            var result = await _tokenRelationRepository.GetListAsync(Filter, skip: skipCount, limit: maxResultCount);

            list = result.Item2;
            var count = list.Count;
            res.AddRange(list);
            if (list.IsNullOrEmpty() || count < maxResultCount)
            {
                break;
            }

            skipCount += count;
        } while (!list.IsNullOrEmpty());

        return res;
    }

    private async Task<List<ThirdTokenIndex>> GetAllThirdTokenListAsync(List<string> thirdToken,
        List<string> thirdChain)
    {
        if (thirdToken.IsNullOrEmpty() || thirdChain.IsNullOrEmpty())
        {
            return new List<ThirdTokenIndex>();
        }

        var res = new List<ThirdTokenIndex>();
        var skipCount = 0;
        var maxResultCount = 5000;
        List<ThirdTokenIndex> list;
        do
        {
            var mustQuery = new List<Func<QueryContainerDescriptor<ThirdTokenIndex>, QueryContainer>>();

            mustQuery.Add(q => q.Terms(i => i.Field(f => f.TokenName).Terms(thirdToken)));
            mustQuery.Add(q => q.Terms(i => i.Field(f => f.Chain).Terms(thirdChain)));

            QueryContainer Filter(QueryContainerDescriptor<ThirdTokenIndex> f) => f.Bool(b => b.Must(mustQuery));
            var result = await _repository.GetListAsync(Filter, skip: skipCount, limit: maxResultCount);

            list = result.Item2;
            var count = list.Count;
            res.AddRange(list);
            if (list.IsNullOrEmpty() || count < maxResultCount)
            {
                break;
            }

            skipCount += count;
        } while (!list.IsNullOrEmpty());

        return res;
    }
}