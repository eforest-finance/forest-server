using System.Collections.Generic;

namespace NFTMarketServer.RemoteClient;

public class EthApiGetContractCreationDto
{
    public string Status { get; set; }
    public string Message { get; set; }
    public List<ContractCreation> result { get; set; }
}

public class ContractCreation
{
    public string ContractAddress { get; set; }
    public string ContractCreator { get; set; }
    public string TxHash { get; set; }
}