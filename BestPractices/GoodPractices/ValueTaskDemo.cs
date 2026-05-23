namespace BestPractices.GoodPractices;

/// <summary>
/// 最佳实践：ValueTask 的正确使用场景
/// 
/// ValueTask 是 Task 的值类型版本，在特定场景下可以减少内存分配
/// 但用错了反而比 Task 还慢！
/// </summary>
public class ValueTaskDemo
{
    private static readonly Dictionary<int, string> _cache = new()
    {
        [1] = "缓存用户_1",
        [2] = "缓存用户_2",
    };

    // ❌ 反模式 1：所有方法无脑换成 ValueTask
    // ValueTask 有额外的复杂性，如果每次都是真正的异步，用 Task 反而更好
    public static async ValueTask<string> BadAlwaysValueTask(int id)
    {
        // 这里每次都要真正 await，ValueTask 的优势发挥不出来
        await Task.Delay(100);
        return $"用户_{id}";
    }

    // ❌ 反模式 2：多次 await 同一个 ValueTask（未定义行为！）
    public static async Task BadAwaitValueTaskMultipleTimes()
    {
        var valueTask = GetCachedUserAsync(1);

        // ❌ 不能 await 两次！ValueTask 不支持多次 await
        var result1 = await valueTask;
        // var result2 = await valueTask; // 这会导致未定义行为或异常！
        Console.WriteLine($"  只能 await 一次: {result1}");
    }

    // ✅ ValueTask 的黄金使用场景：
    // 方法有可能同步完成（缓存命中），也有可能需要异步操作（缓存未命中）
    public static ValueTask<string> GetCachedUserAsync(int id)
    {
        // 缓存命中：同步路径，零分配！
        if (_cache.TryGetValue(id, out var cached))
        {
            Console.WriteLine($"  缓存命中！直接同步返回: {cached}");
            return ValueTask.FromResult(cached); // 不分配 Task 对象
        }

        // 缓存未命中：走异步路径
        return new ValueTask<string>(FetchAndCacheAsync(id));
    }

    private static async Task<string> FetchAndCacheAsync(int id)
    {
        Console.WriteLine($"  缓存未命中，异步获取用户_{id}...");
        await Task.Delay(100).ConfigureAwait(false);
        var user = $"用户_{id}";
        _cache[id] = user;
        return user;
    }

    // ✅ 另一个好场景：高频调用的方法，大多数情况下同步完成
    // 比如检查队列是否有数据
    private static readonly Queue<string> _queue = new();

    public static ValueTask<string?> TryDequeueAsync()
    {
        // 大多数时候队列里有数据，同步返回
        if (_queue.TryDequeue(out var item))
        {
            return ValueTask.FromResult<string?>(item); // 零分配
        }

        // 少数情况下需要等待
        return new ValueTask<string?>(WaitForItemAsync());
    }

    private static async Task<string?> WaitForItemAsync()
    {
        await Task.Delay(50).ConfigureAwait(false);
        return _queue.TryDequeue(out var item) ? item : null;
    }

    // ✅ 对比演示：高频调用场景下 Task vs ValueTask 的分配差异
    public static async Task BenchmarkComparison()
    {
        const int iterations = 100;

        Console.WriteLine($"\n  模拟 {iterations} 次缓存查询（缓存命中 vs 未命中）：");

        // 预热缓存
        _cache[3] = "缓存用户_3";

        int cacheHits = 0;
        int cacheMisses = 0;

        for (int i = 0; i < iterations; i++)
        {
            // 70% 缓存命中，30% 缓存未命中
            int id = (i % 10 < 7) ? (i % 3 + 1) : (i + 100);
            var user = await GetCachedUserAsync(id);
            if (user.StartsWith("缓存")) cacheHits++;
            else cacheMisses++;
        }

        Console.WriteLine($"  结果 - 缓存命中: {cacheHits}，缓存未命中: {cacheMisses}");
        Console.WriteLine("  缓存命中时 ValueTask 零分配，性能比 Task 更好！");
    }

    public static async Task Demo()
    {
        Console.WriteLine("\n=== ValueTask 最佳实践演示 ===");

        Console.WriteLine("\n✅ ValueTask 的黄金场景（缓存查询）：");
        var user1 = await GetCachedUserAsync(1); // 缓存命中，同步
        Console.WriteLine($"  用户: {user1}");

        var user99 = await GetCachedUserAsync(99); // 缓存未命中，异步
        Console.WriteLine($"  用户: {user99}");

        var user99Again = await GetCachedUserAsync(99); // 现在缓存命中了
        Console.WriteLine($"  再次查询: {user99Again}");

        Console.WriteLine("\n⚠️ ValueTask 不能多次 await：");
        await BadAwaitValueTaskMultipleTimes();

        Console.WriteLine("\n📌 ValueTask 使用原则：");
        Console.WriteLine("  ✅ 用 ValueTask：方法经常同步完成（缓存、快路径）");
        Console.WriteLine("  ✅ 用 ValueTask：高频调用，分配成本显著");
        Console.WriteLine("  ❌ 不用 ValueTask：总是真正异步的操作（用 Task）");
        Console.WriteLine("  ❌ 不用 ValueTask：需要多次 await 或存储到变量后延迟 await");
        Console.WriteLine("  ❌ 不用 ValueTask：用于类字段/属性存储（用 Task）");
    }
}
