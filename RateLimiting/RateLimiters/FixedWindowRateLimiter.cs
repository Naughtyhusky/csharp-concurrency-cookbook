namespace RateLimiting.RateLimiters;

/// <summary>
/// 固定窗口限流器
/// 优点：实现简单，内存占用低
/// 缺点：边界突刺问题
/// </summary>
public class FixedWindowRateLimiter(int limit, TimeSpan window)
{
    private int _count;
    private DateTime _windowStart = DateTime.UtcNow;
    private readonly int _limit = limit;
    private readonly TimeSpan _window = window;
    private readonly Lock _lock = new();

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // 窗口过期，重置计数器
            if (now - _windowStart >= _window)
            {
                _count = 0;
                _windowStart = now;
            }

            // 检查是否超过限制
            if (_count < _limit)
            {
                _count++;
                return true;
            }

            return false;
        }
    }

    public int GetCurrentCount()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            if (now - _windowStart >= _window)
            {
                return 0;
            }
            return _count;
        }
    }

    public TimeSpan GetTimeUntilReset()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;
            var elapsed = now - _windowStart;
            return elapsed >= _window ? TimeSpan.Zero : _window - elapsed;
        }
    }
}
