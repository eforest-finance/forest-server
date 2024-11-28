using System;
using System.Collections.Generic;
using System.Transactions;
using Orleans;

namespace NFTMarketServer.Dealer.Dtos;

[GenerateSerializer]
public class ContractInvokeGrainDto
{
    [Id(0)]
    public Guid Id { get; set; }

    [Id(1)]
    public string CreateTime { get; set; }

    [Id(2)]
    public string UpdateTime { get; set; }

    // uniq id of bizData
    [Id(3)]
    public string BizId { get; set; }

    // bizTypeName
    [Id(4)]
    public string BizType { get; set; }

    // which chainId to invoke
    [Id(5)]
    public string ChainId { get; set; }

    // which contract to invoke
    [Id(6)]
    public string ContractName { get; set; }

    // which contract method to invoke
    [Id(7)]
    public string ContractMethod { get; set; }

    // account of invoker
    [Id(8)]
    public string Sender { get; set; }

    /// transactionId
    [Id(9)]
    public string TransactionId { get; set; }

    /// send status <see cref="SendStatus"/>
    [Id(10)]
    public string Status { get; set; }

    /// transaction status <see cref="TransactionResultStatus"/>
    [Id(11)]
    public string TransactionStatus { get; set; }

    /// ByteString.toBase64 value of <see cref="Transaction"/> with Signature
    [Id(12)]
    public string RawTransaction { get; set; }

    /// ByteString value of contract param protobuf IMessage object
    [Id(13)]
    public string Param { get; set; }

    /// Json of <see cref="TransactionResultDto" />
    [Id(14)]
    public string TransactionResult { get; set; }

    [Id(15)]
    public List<TransactionStatus> TransactionStatusFlow { get; set; } = new();

    [Id(16)]
    public int ExecutionCount { get; set; }

}