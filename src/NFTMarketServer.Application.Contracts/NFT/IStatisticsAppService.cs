using System.Threading.Tasks;
using Volo.Abp.Application.Dtos;

namespace NFTMarketServer.NFT;

public interface IStatisticsAppService
{
    Task<long> GetListAsync(GetNewUserInput input);
}