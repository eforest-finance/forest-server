using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Google.Protobuf;
using Nest;
using NFTMarketServer.Common;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Dealer.Etos;
using NFTMarketServer.Dealer.Index;
using NFTMarketServer.Grains.Grain.Dealer.ContractInvoker;
using Orleans;
using Volo.Abp;
using Volo.Abp.DependencyInjection;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.Dealer.Provider;

public class ContractInvokeProvider : ISingletonDependency
{
    private readonly IClusterClient _clusterClient;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly IObjectMapper _objectMapper;
    private readonly INESTRepository<ContractInvokeIndex, Guid> _contractInvokeRepository;

    public ContractInvokeProvider(
        IClusterClient clusterClient,
        IDistributedEventBus distributedEventBus,
        IObjectMapper objectMapper,
        INESTRepository<ContractInvokeIndex, Guid> contractInvokeRepository)
    {
        _clusterClient = clusterClient;
        _distributedEventBus = distributedEventBus;
        _objectMapper = objectMapper;
        _contractInvokeRepository = contractInvokeRepository;
    }

    
    public async Task<List<ContractInvokeIndex>> QueryPendingResult(string updateTimeLt, int pageSize)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<ContractInvokeIndex>, QueryContainer>>
        {
            q => q.TermRange(i => i.Field(f => f.UpdateTime).LessThan(updateTimeLt)),
            q => q.Terms(i => i.Field(f => f.TransactionStatus).Terms(TransactionResultStatus.PENDING.ToString()))
        };
        QueryContainer Filter(QueryContainerDescriptor<ContractInvokeIndex> f) => f.Bool(b => b.Must(mustQuery));
        IPromise<IList<ISort>> Sort(SortDescriptor<ContractInvokeIndex> s) => s.Descending(t => t.UpdateTime);
        var (totalCount, notifyRulesIndices) =
            await _contractInvokeRepository.GetSortListAsync(Filter, sortFunc: Sort, limit: pageSize);
        return notifyRulesIndices;
    }

    
    public async Task<ContractInvokeGrainDto> CreateAsync(ContractParamDto paramDto)
    {
        var grain = _clusterClient.GetGrain<IContractInvokeGrain>( GuidHelper.UniqId(paramDto.BizType, paramDto.BizId));
        var grainDto = (await grain.GetAsync()).Data; 
        _objectMapper.Map(paramDto, grainDto);
        grainDto.Status = ContractInvokeSendStatus.NotSend.ToString();
        grainDto.Param = paramDto.BizData;
        return await AddUpdateAsync(grainDto);
    }

    public async Task<ContractInvokeGrainDto> AddUpdateAsync(ContractInvokeGrainDto paramDto)
    {
        var grainDto = paramDto;
        var uniqId = GuidHelper.UniqId(grainDto.BizType, grainDto.BizId);
        var grain = _clusterClient.GetGrain<IContractInvokeGrain>(uniqId);
        var grainResult = await grain.UpdateAsync(grainDto);
        AssertHelper.IsTrue(grainResult.Success, "Grain update fail");
        if (!grainResult.Success)
            throw new UserFriendlyException("grain update fail");

        // publish changed event, save data to ES
        await _distributedEventBus.PublishAsync(new ContractInvokeChangedEto
        {
            ContractInvokeGrainDto = grainResult.Data
        }, false);
        return grainResult.Data;
    }

    public async Task<ContractInvokeGrainDto> GetByIdAsync(string bizType, string bizId)
    {
        var uniqId = GuidHelper.UniqId(bizType, bizId);
        var grain = _clusterClient.GetGrain<IContractInvokeGrain>(uniqId);
        return (await grain.GetAsync()).Data;
    }

}