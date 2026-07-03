namespace RateLimiting.RateLimiters;

/// <summary>
/// 令牌桶限流器（推荐）
/// 优点：允许短时间突发流量，平滑限流，内存占用低
/// 缺点：实现稍复杂
/// </summary>
public class TokenBucketRateLimiter(int capacity, double refillRate)
{
    private double _tokens = capacity;
    private DateTime _lastRefill = DateTime.UtcNow;
    private readonly int _capacity = capacity;
    private readonly double _refillRate = refillRate; // 每秒添加的令牌数
    private readonly Lock _lock = new();

    public bool TryAcquire(int tokensRequired = 1)
    {
        lock (_lock)
        {
            Refill();

            if (_tokens >= tokensRequired)
            {
                _tokens -= tokensRequired;
                return true;
            }

            return false;
        }
    }

    private void Refill()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;

        // 补充令牌（不超过容量）
        _tokens = Math.Min(_capacity, _tokens + elapsed * _refillRate);
        _lastRefill = now;
    }

    public double GetAvailableTokens()
    {
        lock (_lock)
        {
            Refill();
            return _tokens;
        }
    }

    public TimeSpan GetTimeUntilNextToken()
    {
        lock (_lock)
        {
            Refill();

            if (_tokens >= 1)
            {
                return TimeSpan.Zero;
            }

            var tokensNeeded = 1 - _tokens;
            var secondsNeeded = tokensNeeded / _refillRate;
            return TimeSpan.FromSeconds(secondsNeeded);
        }
    }
}
