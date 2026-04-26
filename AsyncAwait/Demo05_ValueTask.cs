using System.Diagnostics;

namespace AsyncAwait;

/// <summary>
/// Demo05: ValueTask 性能优化
/// </summary>
public static class Demo05_ValueTask
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== Demo05: ValueTask 性能优化 ===\n");

        // 5.1 Task vs ValueTask 对比
        Console.WriteLine("--- 5.1 Task<T> vs ValueTask<T> 对比 ---");
        await TaskVsValueTaskComparison();

        // 5.2 缓存场景
        Console.WriteLine("\n--- 5.2 缓存场景（ValueTask 的优势）---");
        await CachedScenario();

        // 5.3 ValueTask 的限制
        Console.WriteLine("\n--- 5.3 ValueTask 的使用限制 ---");
        await ValueTaskLimitations();
    }

    static async Task TaskVsValueTaskComparison()
    {
        var service = new DataService();

        // 预热
        _ = await service.GetWithTaskAsync(1);
        _ = await service.GetWithValueTaskAsync(1);

        const int iterations = 10000;

        // 测试 Task<T>
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _ = await service.GetWithTaskAsync(i % 100);
        }
        sw1.Stop();

        // 测试 ValueTask<T>
        var sw2 = Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
        {
            _ = await service.GetWithValueTaskAsync(i % 100);
        }
        sw2.Stop();

        Console.WriteLine($"✓ Task<T>: {sw1.ElapsedMilliseconds} ms");
        Console.WriteLine($"✓ ValueTask<T>: {sw2.ElapsedMilliseconds} ms");
        Console.WriteLine($"  💡 性能提升: {(double)sw1.ElapsedMilliseconds / sw2.ElapsedMilliseconds:F2}x");
        Console.WriteLine($"  💡 缓存命中率: 90%（大部分调用无堆分配）");
    }

    static async Task CachedScenario()
    {
        var service = new DataService();

        // 场景 1：缓存命中（同步返回）
        Console.WriteLine("✓ 场景 1: 缓存命中");
        var result1 = await service.GetWithValueTaskAsync(42);
        Console.WriteLine($"  结果: {result1}");

        // 场景 2：缓存未命中（异步获取）
        Console.WriteLine("\n✓ 场景 2: 缓存未命中");
        var result2 = await service.GetWithValueTaskAsync(999);
        Console.WriteLine($"  结果: {result2}");
    }

    static async Task ValueTaskLimitations()
    {
        var service = new DataService();

        Console.WriteLine("✓ ValueTask 的正确用法：");

        // ✅ 正确：只 await 一次
        ValueTask<int> task1 = service.GetWithValueTaskAsync(1);
        int result1 = await task1;
        Console.WriteLine($"  ✅ 只 await 一次: {result1}");

        Console.WriteLine("\n✓ ValueTask 的错误用法（演示）：");

        // ❌ 错误：多次 await（不要这样做！）
        ValueTask<int> task2 = service.GetWithValueTaskAsync(2);
        try
        {
            int r1 = await task2;
            int r2 = await task2; // ⚠️ 未定义行为
            Console.WriteLine($"  ❌ 多次 await: {r1}, {r2}（可能出错）");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 错误: {ex.Message}");
        }

        // ✅ 正确：转换为 Task 后可以多次使用
        Console.WriteLine("\n✓ 如果需要多次使用，转换为 Task：");
        ValueTask<int> task3 = service.GetWithValueTaskAsync(3);
        Task<int> regularTask = task3.AsTask(); // 转换为 Task
        int r3 = await regularTask;
        int r4 = await regularTask; // ✅ 现在可以多次 await
        Console.WriteLine($"  ✅ 转换后多次 await: {r3}, {r4}");
    }
}

/// <summary>
/// 示例：数据服务（演示 Task 和 ValueTask）
/// </summary>
public class DataService
{
    private readonly Dictionary<int, int> _cache = new();

    public DataService()
    {
        // 预填充缓存（模拟 90% 缓存命中率）
        for (int i = 0; i < 100; i++)
        {
            _cache[i] = i * 10;
        }
    }

    // 使用 Task<T>
    public async Task<int> GetWithTaskAsync(int key)
    {
        // 缓存命中
        if (_cache.TryGetValue(key, out int cached))
        {
            return cached; // ⚠️ 编译器生成 Task.FromResult(cached)，有堆分配
        }

        // 缓存未命中，模拟异步获取
        await Task.Delay(10);
        int value = key * 10;
        _cache[key] = value;
        return value;
    }

    // 使用 ValueTask<T>
    public async ValueTask<int> GetWithValueTaskAsync(int key)
    {
        // 缓存命中
        if (_cache.TryGetValue(key, out int cached))
        {
            return cached; // ✅ 直接返回值，无堆分配
        }

        // 缓存未命中，模拟异步获取
        await Task.Delay(10);
        int value = key * 10;
        _cache[key] = value;
        return value;
    }
}
