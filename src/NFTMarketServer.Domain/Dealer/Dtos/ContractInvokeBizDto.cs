
namespace NFTMarketServer.Dealer.Dtos;

public class ContractInvokeBizDto<T>
{
    // uniq id of bizData
    public string BizId { get; set; }
    
    /// <see cref="Dtos.BizType"/> bizTypeName 
    public string BizType { get; set; }

    // which chainId to invoke
    public string ChainId { get; set; }
    
    // which contract to invoke
    public string ContractName { get; set; }
    
    // which contract method to invoke
    public string ContractMethod { get; set; }
    
    // account of invoker
    public string Sender { get; set; }  
     
    // biz data
    public T BizData { get; set; }

    public string CrossChainId { get; set; }

}