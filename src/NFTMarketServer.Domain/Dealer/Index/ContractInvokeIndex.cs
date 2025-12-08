using System;
using System.Collections.Generic;
using System.Transactions;
using AElf.Indexing.Elasticsearch;
using Nest;
using NFTMarketServer.Dealer.Dtos;
using NFTMarketServer.Entities;
using TransactionStatus = NFTMarketServer.Dealer.Dtos.TransactionStatus;

namespace NFTMarketServer.Dealer.Index;

public class ContractInvokeIndex : NFTMarketEntity<Guid>, IIndexBuild
{
    [Keyword] public string CreateTime { get; set; }
    
    [Keyword] public string UpdateTime { get; set; }
    
    // uniq id of bizData
    [Keyword] public string BizId { get; set; }
    
    // bizTypeName
    [Keyword] public string BizType { get; set; }
    
    // which chainId to invoke
    [Keyword] public string ChainId { get; set; }

    // which contract to invoke
    [Keyword] public string ContractName { get; set; }
    
    // which contract method to invoke
    [Keyword] public string ContractMethod { get; set; }
    
    // account of invoker
    [Keyword] public string Sender { get; set; }  
    
    /// transactionId
    [Keyword] public string TransactionId { get; set; }
    
    /// send status <see cref="ContractInvokeSendStatus"/>
    [Keyword] public string Status { get; set; }
    
    /// transaction status <see cref="TransactionResultStatus"/>
    [Keyword] public string TransactionStatus { get; set; }
    
    /// ByteString value of <see cref="Transaction"/> with Signature
    public string RawTransaction { get; set; }
    
    /// ByteString value of contract param protobuf IMessage object
    public string Param { get; set; } 
    
    /// Json of <see cref="TransactionResultDto" />
    public string TransactionResult { get; set; }
    
    public List<TransactionStatus> TransactionStatusFlow { get; set; } = new();
    
}