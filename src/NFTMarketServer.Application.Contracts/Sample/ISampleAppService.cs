using System.Threading.Tasks;

namespace NFTMarketServer.Sample;

public interface ISampleAppService
{
    Task<string> Hello(string from, string to);
}