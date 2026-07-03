using StackExchange.Redis;

namespace RateLimiting.DistributedLimiters;

/// <summary>
/// 基于 Redis 的分布式令牌桶限流器
/// 适用于多实例部署的场景
/// </summary>
public class RedisTokenBucketRateLimiter(IConnectionMultiplexer redis, int capacity, double refillRate)
{
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly int _capacity = capacity;
    private readonly double _refillRate = refillRate;

    /// <summary>
    /// 尝试获取令牌
    /// </summary>
    /// <param name="key">限流的键（如用户ID、IP地址）</param>
    /// <param name="tokensRequired">需要的令牌数（默认1）</param>
    /// <returns>是否获取成功</returns>
    public async Task<bool> TryAcquireAsync(string key, int tokensRequired = 1)
    {
        var db = _redis.GetDatabase();

        // 使用 Lua 脚本确保原子性
        var script = @"
            local key = KEYS[1]
            local capacity = tonumber(ARGV[1])
            local refill_rate = tonumber(ARGV[2])
            local tokens_required = tonumber(ARGV[3])
            local now = tonumber(ARGV[4])

            -- 获取当前令牌数和上次补充时间
            local bucket = redis.call('HMGET', key, 'tokens', 'last_refill')
            local tokens = tonumber(bucket[1]) or capacity
            local last_refill = tonumber(bucket[2]) or now

            -- 计算需要补充的令牌
            local elapsed = now - last_refill
            tokens = math.min(capacity, tokens + elapsed * refill_rate)

            -- 尝试消费令牌
            if tokens >= tokens_required then
                tokens = tokens - tokens_required
                redis.call('HMSET', key, 'tokens', tokens, 'last_refill', now)
                redis.call('EXPIRE', key, 60)  -- 设置过期时间
                return 1  -- 成功
            else
                redis.call('HMSET', key, 'tokens', tokens, 'last_refill', now)
                redis.call('EXPIRE', key, 60)
                return 0  -- 失败
            end
        ";

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var result = await db.ScriptEvaluateAsync(
            script,
            keys: [key],
            values: [_capacity, _refillRate, tokensRequired, now]
        );

        return (int)result == 1;
    }

    /// <summary>
    /// 获取当前可用令牌数
    /// </summary>
    public async Task<double> GetAvailableTokensAsync(string key)
    {
        var db = _redis.GetDatabase();

        var bucket = await db.HashGetAllAsync(key);
        if (bucket.Length == 0)
        {
            return _capacity;
        }

        var tokens = double.Parse(bucket.FirstOrDefault(h => h.Name == "tokens").Value);
        var lastRefill = double.Parse(bucket.FirstOrDefault(h => h.Name == "last_refill").Value);

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() / 1000.0;
        var elapsed = now - lastRefill;

        return Math.Min(_capacity, tokens + elapsed * _refillRate);
    }
}
