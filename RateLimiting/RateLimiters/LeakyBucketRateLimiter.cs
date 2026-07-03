namespace RateLimiting.RateLimiters;

/// <summary>
/// 漏桶限流器
/// 优点：输出速率平滑，保护下游系统
/// 缺点：无法应对突发流量
/// </summary>
public class LeakyBucketRateLimiter(int capacity, TimeSpan leakInterval)
{
    private readonly Queue<DateTime> _queue = new();
    private readonly int _capacity = capacity;
    private readonly TimeSpan _leakInterval = leakInterval; // 漏出间隔
    private DateTime _lastLeak = DateTime.UtcNow;
    private readonly Lock _lock = new();

    public bool TryAcquire()
    {
        lock (_lock)
        {
            Leak();

            if (_queue.Count < _capacity)
            {
                _queue.Enqueue(DateTime.UtcNow);
                return true;
            }

            return false;
        }
    }

    private void Leak()
    {
        var now = DateTime.UtcNow;
        var elapsed = now - _lastLeak;

        // 计算应该漏出多少个请求
        var leakCount = (int)(elapsed / _leakInterval);
        for (int i = 0; i < leakCount && _queue.Count > 0; i++)
        {
            _queue.Dequeue();
        }

        if (leakCount > 0)
        {
            _lastLeak = now;
        }
    }

    public int GetCurrentCount()
    {
        lock (_lock)
        {
            Leak();
            return _queue.Count;
        }
    }

    public int GetAvailableSlots()
    {
        lock (_lock)
        {
            Leak();
            return _capacity - _queue.Count;
        }
    }
}
