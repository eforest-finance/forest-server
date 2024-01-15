using System;
using System.Collections.Generic;
using System.Transactions;

namespace NFTMarketServer.Dealer.Dtos;

public class ContractInvokeGrainDto
{
    
    public Guid Id { get; set; }
    
    public string CreateTime { get; set; }
    
    public string UpdateTime { get; set; }
    
    // uniq id of bizData
    public string BizId { get; set; }
    
    // bizTypeName
    public string BizType { get; set; }
    
    // which chainId to invoke
    public string ChainId { get; set; }

    // which contract to invoke
    public string ContractName { get; set; }
    
    // which contract method to invoke
    public string ContractMethod { get; set; }
    
    // account of invoker
    public string Sender { get; set; }  

    /// transactionId
    public string TransactionId { get; set; }
    
    /// send status <see cref="SendStatus"/>
    public string Status { get; set; }
    
    /// transaction status <see cref="TransactionResultStatus"/>
    public string TransactionStatus { get; set; }
    
    /// ByteString.toBase64 value of <see cref="Transaction"/> with Signature
    public string RawTransaction { get; set; }

    /// ByteString value of contract param protobuf IMessage object
    public string Param { get; set; } 
    
    /// Json of <see cref="TransactionResultDto" />
    public string TransactionResult { get; set; }
    
    public List<TransactionStatus> TransactionStatusFlow { get; set; } = new();

    public int ExecutionCount { get; set; }

}