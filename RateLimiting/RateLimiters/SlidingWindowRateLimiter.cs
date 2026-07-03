namespace RateLimiting.RateLimiters;

/// <summary>
/// 滑动窗口限流器
/// 优点：精确限流，无边界突刺问题
/// 缺点：内存占用高（需要记录每个请求的时间戳）
/// </summary>
public class SlidingWindowRateLimiter(int limit, TimeSpan window)
{
    private readonly Queue<DateTime> _timestamps = new();
    private readonly int _limit = limit;
    private readonly TimeSpan _window = window;
    private readonly Lock _lock = new();

    public bool TryAcquire()
    {
        lock (_lock)
        {
            var now = DateTime.UtcNow;

            // 移除过期的时间戳
            while (_timestamps.Count > 0 && now - _timestamps.Peek() >= _window)
            {
                _timestamps.Dequeue();
            }

            // 检查是否超过限制
            if (_timestamps.Count < _limit)
            {
                _timestamps.Enqueue(now);
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

            // 移除过期的时间戳
            while (_timestamps.Count > 0 && now - _timestamps.Peek() >= _window)
            {
                _timestamps.Dequeue();
            }

            return _timestamps.Count;
        }
    }

    public DateTime? GetOldestTimestamp()
    {
        lock (_lock)
        {
            return _timestamps.Count > 0 ? _timestamps.Peek() : null;
        }
    }
}
