using Google.Protobuf;

namespace NFTMarketServer.Dealer.Dtos;

/// <summary>
///
///     e.g:
///         new ContractParamDto() {
///             ...
///             BizData = new TransferInput().ToByteString().ToBase64()
///             ...
///         }
/// </summary>
public class ContractParamDto : ContractInvokeBizDto<string>
{
    
}