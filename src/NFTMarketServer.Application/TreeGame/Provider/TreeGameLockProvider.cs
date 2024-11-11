using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Volo.Abp.Caching;
using Volo.Abp.DependencyInjection;

namespace NFTMarketServer.TreeGame.Provider;

public class TreeGameLockProvider : ITreeGameLockProvider, ISingletonDependency
{
    private readonly ILogger<TreeGameLockProvider> _logger;
    private const string TreeUserCatchKeyPrefix = "tree_user_";
    private const long CatchExpireTime = 10000;//10s
    private const long LockWaitTimeout = 11000;//s
    private readonly IDistributedCache<TreeUserCacheItem> _treeCache;
    private readonly Random _random = new Random();
    public TreeGameLockProvider(ILogger<TreeGameLockProvider> logger,IDistributedCache<TreeUserCacheItem> treeCache)
    {
        _logger = logger;
        _treeCache = treeCache;

    }

    public async Task<bool> TryAcquireLockAsync(string address)
    {
        var endTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow) + LockWaitTimeout;
        var attemptDelay = TimeSpan.FromMilliseconds(_random.Next(50, 500));
        var lockKey = string.Concat(TreeUserCatchKeyPrefix, address);
        while (DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow) < endTime)
        {
            var value = await _treeCache.GetAsync(lockKey);
            if (value == null || value.LockTime == 0)
            {
                var lockValue = new TreeUserCacheItem() { LockTime = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)};
                var cacheOptions = new DistributedCacheEntryOptions
                {
                    SlidingExpiration = TimeSpan.FromSeconds(CatchExpireTime/1000)
                };
                _treeCache.SetAsync(lockKey, lockValue, cacheOptions);
                return true;
            }
            await Task.Delay(attemptDelay);
        }
        return false;
    }

    public async Task ReleaseLockAsync(string address)
    {
        var lockKey = string.Concat(TreeUserCatchKeyPrefix, address);
        var value = await _treeCache.GetAsync(lockKey);
        if (value != null && value.LockTime != 0)
        {
            await _treeCache.RemoveAsync(lockKey);
        }

    }
}
