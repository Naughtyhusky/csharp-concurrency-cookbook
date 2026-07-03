using StackExchange.Redis;

namespace RateLimiting.DistributedLimiters;

/// <summary>
/// 基于 Redis 的分布式滑动窗口限流器
/// 使用 Sorted Set 实现
/// </summary>
public class RedisSlidingWindowRateLimiter(IConnectionMultiplexer redis, int limit, TimeSpan window)
{
    private readonly IConnectionMultiplexer _redis = redis;
    private readonly int _limit = limit;
    private readonly TimeSpan _window = window;

    /// <summary>
    /// 尝试获取配额
    /// </summary>
    public async Task<bool> TryAcquireAsync(string key)
    {
        var db = _redis.GetDatabase();

        // 使用 Lua 脚本确保原子性
        var script = @"
            local key = KEYS[1]
            local window = tonumber(ARGV[1])
            local limit = tonumber(ARGV[2])
            local now = tonumber(ARGV[3])

            -- 移除过期的时间戳
            redis.call('ZREMRANGEBYSCORE', key, 0, now - window)

            -- 获取当前窗口内的请求数
            local count = redis.call('ZCARD', key)

            if count < limit then
                -- 添加当前时间戳
                redis.call('ZADD', key, now, now)
                redis.call('EXPIRE', key, window)
                return 1
            else
                return 0
            end
        ";

        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = (long)_window.TotalMilliseconds;

        var result = await db.ScriptEvaluateAsync(
            script,
            keys: [key],
            values: [windowMs, _limit, now]
        );

        return (int)result == 1;
    }

    /// <summary>
    /// 获取当前窗口内的请求数
    /// </summary>
    public async Task<long> GetCurrentCountAsync(string key)
    {
        var db = _redis.GetDatabase();
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        var windowMs = (long)_window.TotalMilliseconds;

        // 先清理过期数据
        await db.SortedSetRemoveRangeByScoreAsync(key, 0, now - windowMs);

        // 返回当前数量
        return await db.SortedSetLengthAsync(key);
    }
}
