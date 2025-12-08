using System;
using NFTMarketServer.Basic;
using StackExchange.Redis;
namespace NFTMarketServer.Redis;


public interface IOpenAiRedisTokenBucket
{
    public int GetNextToken();
}

public class OpenAiRedisTokenBucket : IOpenAiRedisTokenBucket
{
    private readonly IDatabase _redisDatabase;
    private readonly string _key;
    private readonly int _maxValue;

    public OpenAiRedisTokenBucket(IDatabase redisDatabase, string key, int maxValue = 0)
    {
        _redisDatabase = redisDatabase;
        _key = key;
        _maxValue = Math.Max(CommonConstant.IntZero, maxValue);
    }

    public int GetNextToken()
    {
        // Lua script to increment and return the token value atomically
        var script = @"
            local newValue = redis.call('INCR', KEYS[1])
            if newValue > tonumber(ARGV[1]) then
                redis.call('SET', KEYS[1], 0)
                newValue = 0
            end
            return newValue
        ";

        // Execute Lua script
        var token = (int)(long)_redisDatabase.ScriptEvaluate(script, new RedisKey[] { _key }, new RedisValue[] { _maxValue });

        return token;
    }
}