using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Google.Protobuf.WellKnownTypes;
using Nest;
using NFTMarketServer.Grains;
using NFTMarketServer.Grains.Grain.Verify;
using NFTMarketServer.OwnerShip.Dto;
using NFTMarketServer.OwnerShip.Verify;
using NFTMarketServer.Symbol;
using NFTMarketServer.Symbol.Etos;
using NFTMarketServer.Symbol.Index;
using Orleans;
using Volo.Abp;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.ObjectMapping;

namespace NFTMarketServer.OwnerShip;

[RemoteService(IsEnabled = false )]
public class VerifyAppService : ApplicationService, IVerifyAppService
{
    private readonly IEnumerable<IOwnerShipVerify> _ownerShipVerifies;
    private readonly IClusterClient _clusterClient;
    private readonly IObjectMapper _objectMapper;
    private readonly IDistributedEventBus _distributedEventBus;
    private readonly INESTRepository<OwnerShipVerifyOrderIndex, Guid> _ownerShipVerifyOrderReposity;

    public VerifyAppService(IEnumerable<IOwnerShipVerify> ownerShipVerifies, IClusterClient clusterClient, IObjectMapper objectMapper, IDistributedEventBus distributedEventBus, INESTRepository<OwnerShipVerifyOrderIndex, Guid> ownerShipVerifyOrderReposity)
    {
        _ownerShipVerifies = ownerShipVerifies;
        _clusterClient = clusterClient;
        _objectMapper = objectMapper;
        _distributedEventBus = distributedEventBus;
        _ownerShipVerifyOrderReposity = ownerShipVerifyOrderReposity;
    }

    private async Task<ReserveSymbolDto> GetReserveSymbolAsync(string chainId, string symbol)
    {
        // todo: need change to search gql
        return new ReserveSymbolDto
        {
            TokenContract = "0xB83c27805aAcA5C7082eB45C868d955Cf04C337F",
            IssueChain = "ETH"
        };
    }

    private MessageInfo RecoverMessageInfo(String message)
    {
        var regex = new Regex(@"\[(.*?)\]");
        var matches = regex.Matches(message);
        if (matches.Count != 4)
        {
            return null;
        }
        var contractAddress = matches[1].Value.Substring(1, matches[1].Value.Length - 2);
        var symbol = matches[2].Value.Substring(1, matches[2].Value.Length - 2);;
        var aelfAddresses = matches[3].Value.Substring(1, matches[3].Value.Length - 2).Split("_");
        if (aelfAddresses.Length != 3)
        {
            return null;
        }

        return new MessageInfo
        {
            Symbol = symbol,
            IssueContractAddress = contractAddress,
            From = matches[3].Value,
            IssueAddress = aelfAddresses[1],
            ChainId = aelfAddresses[2]
        };
    }

    public async Task<AutoVerifyResultDto> OwnerShipVerifyAsync(AutoVerifyInput input)
    {
        var result = new AutoVerifyResultDto();
        var messageInfo = RecoverMessageInfo(input.Message);
        if (messageInfo == null)
        {
            return result;
        }

        var reserveSymbolDto = await GetReserveSymbolAsync(messageInfo.ChainId, messageInfo.Symbol);
        if (reserveSymbolDto == null || reserveSymbolDto.TokenContract != messageInfo.IssueContractAddress)
        {
            return result;
        }

        var ownerShipVerify = _ownerShipVerifies.FirstOrDefault(t => t.IssueChain == reserveSymbolDto.IssueChain);
        if (ownerShipVerify == null)
        {
            return result;
        }

        var publicKey = ownerShipVerify.RecoverPublicKey(input.Signature, input.Message);
        var recoverAddress = ownerShipVerify.GetAddressByPublicKey(publicKey);
        var queryAddress = await ownerShipVerify.FetchCreatorAddressAsync(messageInfo.Symbol, messageInfo.IssueContractAddress);
        result.VerifyResult = recoverAddress.Equals(queryAddress, StringComparison.OrdinalIgnoreCase);
        
        if (result.VerifyResult)
        {
            var ownerShipVerifyOrder = _objectMapper.Map<AutoVerifyInput, OwnerShipVerifyOrder>(input);
            _objectMapper.Map(messageInfo, ownerShipVerifyOrder);
            ownerShipVerifyOrder.VerifyResult = true;
            ownerShipVerifyOrder.ApprovalStatus = ApprovalStatus.AutoApproved;
            ownerShipVerifyOrder.ProposalStatus = ProposalStatus.Unsent;
            ownerShipVerifyOrder.SubmitTime = DateTime.UtcNow.ToTimestamp().Seconds;
            ownerShipVerifyOrder.ApprovalTime = DateTime.UtcNow.ToTimestamp().Seconds;
            await AddOrUpdateOwnerShipVerifyOrderAsync(ownerShipVerifyOrder);
        }
        return result;
    }

    public async Task AddOrUpdateOwnerShipVerifyOrderAsync(OwnerShipVerifyOrder ownerShipVerifyOrder)
    {
        if (ownerShipVerifyOrder.Id == Guid.Empty)
        {
            ownerShipVerifyOrder.Id = Guid.NewGuid();
        }

        if (ownerShipVerifyOrder.ApprovalStatus == ApprovalStatus.AutoApproved && ownerShipVerifyOrder.ProposalStatus == ProposalStatus.Unsent)
        {
            // todo send contract event
            ownerShipVerifyOrder.ProposalStatus = ProposalStatus.Sent;
        }

        var grain = _clusterClient.GetGrain<IOwnerShipVerifyOrderGrain>(GrainIdHelper.GenerateGrainId(ownerShipVerifyOrder.ChainId, ownerShipVerifyOrder.Id));
        var result = await grain.AddOrUpdateAsync(
            _objectMapper.Map<OwnerShipVerifyOrder, OwnerShipVerifyOrderGrainDto>(ownerShipVerifyOrder));
        if (!result.Success)
        {
            return;
        }
        await _distributedEventBus.PublishAsync(_objectMapper.Map<OwnerShipVerifyOrderGrainDto, OwnerShipVerifyOrderEto>(result.Data));
    }

    public async Task<ResultDto> CommitManualVerifyAsync(CommitManualVerifyInput input)
    {
        var result = new ResultDto();
        var messageInfo = RecoverMessageInfo(input.Message);
        if (messageInfo == null)
        {
            return result;
        }
        
        var verifyResult = false;
        foreach (var ownerShipVerify in _ownerShipVerifies)
        {
            var publicKey = ownerShipVerify.RecoverPublicKey(input.Signature, input.Message);
            var recoverAddress = ownerShipVerify.GetAddressByPublicKey(publicKey);
            if (input.ProjectCreatorAddress.Equals(recoverAddress, StringComparison.OrdinalIgnoreCase))
            {
                verifyResult = true;
                break;
            }
        }
        var ownerShipVerifyOrder = _objectMapper.Map<CommitManualVerifyInput, OwnerShipVerifyOrder>(input);
        _objectMapper.Map(messageInfo, ownerShipVerifyOrder);
        ownerShipVerifyOrder.SubmitTime = DateTime.UtcNow.ToTimestamp().Seconds;
        ownerShipVerifyOrder.VerifyResult = verifyResult;
        ownerShipVerifyOrder.ApprovalStatus = ApprovalStatus.Pending;
        ownerShipVerifyOrder.ProposalStatus = ProposalStatus.Unsent;
        await AddOrUpdateOwnerShipVerifyOrderAsync(ownerShipVerifyOrder);
        return new ResultDto
        {
            Result = true
        };
    }

    public async Task<ResultDto> RetryVerifyAsync(ApprovalInput input)
    {
        CheckAdmin();
        var result = new ResultDto();
        var orderIndex = await _ownerShipVerifyOrderReposity.GetAsync(input.Id);
        if (orderIndex == null || orderIndex.VerifyResult)
        {
            return result;
        }
        foreach (var ownerShipVerify in _ownerShipVerifies)
        {
            var publicKey = ownerShipVerify.RecoverPublicKey(orderIndex.Signature, orderIndex.Message);
            var address = ownerShipVerify.GetAddressByPublicKey(publicKey);
            if (address.ToLower() == orderIndex.ProjectCreatorAddress)
            {
                result.Result = true;
                orderIndex.VerifyResult = true;
                await AddOrUpdateOwnerShipVerifyOrderAsync(_objectMapper
                    .Map<OwnerShipVerifyOrderIndex, OwnerShipVerifyOrder>(orderIndex));
                return result;
            }
        }

        return result;
    }

    public async Task<ResultDto> SendProposalAsync(ApprovalInput input)
    {
        CheckAdmin();
        var result = new ResultDto();
        var orderIndex = await _ownerShipVerifyOrderReposity.GetAsync(input.Id);
        if (orderIndex == null || orderIndex.ProposalStatus != ProposalStatus.Unsent)
        {
            return result;
        }
        // todo send event
        orderIndex.ProposalStatus = ProposalStatus.Sent;
        await AddOrUpdateOwnerShipVerifyOrderAsync(_objectMapper
            .Map<OwnerShipVerifyOrderIndex, OwnerShipVerifyOrder>(orderIndex));
        result.Result = true;
        return result;
    }

    public async Task<ResultDto> ApproveVerifyAsync(ApprovalInput input)
    {
        CheckAdmin();
        var result = new ResultDto();
        var orderIndex = await _ownerShipVerifyOrderReposity.GetAsync(input.Id);
        if (orderIndex == null || orderIndex.ApprovalStatus != ApprovalStatus.Pending)
        {
            return result;
        }

        if (orderIndex.ProposalStatus != ProposalStatus.Success)
        {
            throw new Exception("Proposal not success");
        }

        orderIndex.ApprovalStatus = ApprovalStatus.Approved;
        await AddOrUpdateOwnerShipVerifyOrderAsync(_objectMapper
            .Map<OwnerShipVerifyOrderIndex, OwnerShipVerifyOrder>(orderIndex));
        result.Result = true;
        return result;
    }

    public async Task<ResultDto> RejectVerifyAsync(ApprovalInput input)
    {
        CheckAdmin();
        var result = new ResultDto();
        var orderIndex = await _ownerShipVerifyOrderReposity.GetAsync(input.Id);
        if (orderIndex == null || orderIndex.ApprovalStatus != ApprovalStatus.Pending)
        {
            return result;
        }
        orderIndex.ApprovalStatus = ApprovalStatus.Rejected;
        orderIndex.Comment = input.Comment;
        await AddOrUpdateOwnerShipVerifyOrderAsync(_objectMapper
            .Map<OwnerShipVerifyOrderIndex, OwnerShipVerifyOrder>(orderIndex));
        result.Result = true;
        return result;
    }

    public async Task<PagedResultDto<OwnerShipVerifyOrderSummaryDto>> GetOrderSummaryListAsync(GetOrderSummaryListInput input)
    {
        var mustQuery = new List<Func<QueryContainerDescriptor<OwnerShipVerifyOrderIndex>, QueryContainer>>();
        if (input.ApprovalStatus != ApprovalStatus.Unknown)
        {
            mustQuery.Add(q => q.Term(i
                => i.Field(f => f.ApprovalStatus).Value(input.ApprovalStatus)));
        }

        if (!CheckAdmin())
        {
            // todo add issueAddress query
        }
        QueryContainer Filter(QueryContainerDescriptor<OwnerShipVerifyOrderIndex> f) =>
            f.Bool(b => b.Must(mustQuery));
        var orderIndexList = await _ownerShipVerifyOrderReposity.GetListAsync(Filter, skip: input.SkipCount, limit: input.MaxResultCount,
            sortType: SortOrder.Descending, sortExp: o => o.SubmitTime);
        return new PagedResultDto<OwnerShipVerifyOrderSummaryDto>
        {
            Items = _objectMapper.Map<List<OwnerShipVerifyOrderIndex>, List<OwnerShipVerifyOrderSummaryDto>>(orderIndexList.Item2),
            TotalCount = orderIndexList.Item1
        };
    }

    public async Task<OwnerShipVerifyOrderDetailDto> GetVerifyOrderDetailAsync(GetVerifyOrderDetailInput input)
    {
        var orderIndex = await _ownerShipVerifyOrderReposity.GetAsync(input.Id);
        if (orderIndex == null)
        {
            return new OwnerShipVerifyOrderDetailDto();
        }
        // todo check issueAddress

        return _objectMapper.Map<OwnerShipVerifyOrderIndex, OwnerShipVerifyOrderDetailDto>(orderIndex);
    }

    private bool CheckAdmin()
    {
        // todo 
        return true;
    }
}