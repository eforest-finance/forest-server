using System.Threading.Tasks;

namespace NFTMarketServer.Inscription;

public interface IInscriptionAppService
{
    Task<InscribedDto> InscribedAsync(InscribedInput input);
    Task<InscriptionAmountDto> GetInscriptionAsync(GetInscriptionAmountInput input);
}