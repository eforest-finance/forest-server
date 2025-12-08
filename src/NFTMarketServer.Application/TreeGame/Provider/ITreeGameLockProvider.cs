using System.Threading.Tasks;

namespace NFTMarketServer.TreeGame.Provider;

public interface ITreeGameLockProvider
{
    public Task<bool> TryAcquireLockAsync(string address);

    public Task ReleaseLockAsync(string address);

}