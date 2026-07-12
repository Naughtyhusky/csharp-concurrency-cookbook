using System.Collections.Concurrent;

namespace PerformanceOptimization;

/// <summary>
/// ValueTask 示例：减少 Task 分配的性能优化
/// </summary>
public class ValueTaskDemo
{
    private readonly ConcurrentDictionary<string, int> _cache = new();

    public static async Task RunAsync()
    {
        Console.WriteLine("====================================");
        Console.WriteLine("Part 1: ValueTask 示例");
        Console.WriteLine("====================================\n");

        var demo = new ValueTaskDemo();

        // 预填充缓存
        demo._cache["user:1"] = 100;
        demo._cache["user:2"] = 200;

        Console.WriteLine("【场景】：高频缓存查询，大部分请求命中缓存（同步完成）\n");

        // 模拟多次查询
        Console.WriteLine("--- 使用 Task<T>（传统方式）---");
        for (int i = 0; i < 3; i++)
        {
            var result = await demo.GetFromCacheWithTaskAsync($"user:{i + 1}");
            Console.WriteLine($"查询 user:{i + 1} -> {result}");
        }

        Console.WriteLine("\n--- 使用 ValueTask<T>（优化方式）---");
        for (int i = 0; i < 3; i++)
        {
            var result = await demo.GetFromCacheWithValueTaskAsync($"user:{i + 1}");
            Console.WriteLine($"查询 user:{i + 1} -> {result}");
        }

        Console.WriteLine("\n💡 分析：");
        Console.WriteLine("  - Task<T>：即使缓存命中（同步完成），仍会分配 Task 对象到堆上");
        Console.WriteLine("  - ValueTask<T>：缓存命中时，零堆分配！直接返回值");
        Console.WriteLine("  - 高频场景（每秒 10 万+ 次调用）下，ValueTask 可显著减少 GC 压力\n");

        // ValueTask 的使用限制演示
        await demo.DemonstrateValueTaskLimitationsAsync();
    }

    /// <summary>
    /// ❌ 传统方式：使用 Task<T>
    /// 问题：即使缓存命中（同步完成），仍会分配 Task 对象
    /// </summary>
    public async Task<int> GetFromCacheWithTaskAsync(string key)
    {
        // 缓存命中：同步完成，但仍分配 Task
        if (_cache.TryGetValue(key, out var value))
        {
            return value; // 编译器生成：return Task.FromResult(value); 
        }

        // 缓存未命中：模拟异步加载
        await Task.Delay(50);
        var newValue = key.GetHashCode() % 1000;
        _cache[key] = newValue;
        return newValue;
    }

    /// <summary>
    /// ✅ 优化方式：使用 ValueTask<T>
    /// 优势：缓存命中时零堆分配！
    /// </summary>
    public async ValueTask<int> GetFromCacheWithValueTaskAsync(string key)
    {
        // 缓存命中：零分配！直接返回 ValueTask<int>
        if (_cache.TryGetValue(key, out var value))
        {
            return value; // 零分配！ValueTask<T> 是值类型
        }

        // 缓存未命中：异步加载（内部仍使用 Task）
        await Task.Delay(50);
        var newValue = key.GetHashCode() % 1000;
        _cache[key] = newValue;
        return newValue;
    }

    /// <summary>
    /// 演示 ValueTask 的使用限制
    /// </summary>
    private async Task DemonstrateValueTaskLimitationsAsync()
    {
        Console.WriteLine("【ValueTask 的使用限制】\n");

        // ❌ 限制 1：不能多次 await
        Console.WriteLine("❌ 限制 1：不能多次 await ValueTask");
        var vt = GetFromCacheWithValueTaskAsync("user:1");
        await vt; // 第一次 await：✅ 正确
        // await vt; // 第二次 await：❌ 运行时异常！
        Console.WriteLine("  ⚠️  ValueTask 只能 await 一次，多次 await 会抛异常\n");

        // ❌ 限制 2：不能并发 await
        Console.WriteLine("❌ 限制 2：不能并发 await ValueTask");
        Console.WriteLine("  ⚠️  以下代码是错误的：");
        Console.WriteLine("     var vt = GetValueTaskAsync();");
        Console.WriteLine("     await Task.WhenAll(vt.AsTask(), vt.AsTask()); // ❌ 错误！\n");

        // ❌ 限制 3：不能直接用于 WhenAll/WhenAny
        Console.WriteLine("❌ 限制 3：不能直接用于 Task.WhenAll/WhenAny");
        Console.WriteLine("  ⚠️  需要先转换为 Task：");
        var vt1 = GetFromCacheWithValueTaskAsync("user:1");
        var vt2 = GetFromCacheWithValueTaskAsync("user:2");
        // await Task.WhenAll(vt1, vt2); // ❌ 编译错误
        await Task.WhenAll(vt1.AsTask(), vt2.AsTask()); // ✅ 正确
        Console.WriteLine("  ✅ 使用 .AsTask() 转换后可以使用 WhenAll\n");

        // ✅ 正确用法：只 await 一次
        Console.WriteLine("✅ 正确用法：只 await 一次，不缓存 ValueTask");
        var result = await GetFromCacheWithValueTaskAsync("user:1");
        Console.WriteLine($"  结果：{result}\n");
    }

    /// <summary>
    /// 适用场景判断
    /// </summary>
    public static void PrintUsageGuidelines()
    {
        Console.WriteLine("【何时使用 ValueTask？】\n");
        Console.WriteLine("✅ 推荐使用场景：");
        Console.WriteLine("  1. 高频调用的异步方法（每秒 10 万+ 次）");
        Console.WriteLine("  2. 同步完成路径占比 > 50%（如缓存命中率高）");
        Console.WriteLine("  3. 性能敏感的库代码（如 ASP.NET Core、EF Core）\n");

        Console.WriteLine("❌ 不推荐使用场景：");
        Console.WriteLine("  1. 需要多次 await 同一个结果");
        Console.WriteLine("  2. 需要使用 Task.WhenAll/WhenAny");
        Console.WriteLine("  3. 低频调用的方法（Task 分配开销可忽略）");
        Console.WriteLine("  4. 业务代码（优先可读性，Task 够用了）\n");

        Console.WriteLine("📌 经验法则：");
        Console.WriteLine("  - 应用代码：默认用 Task<T>，够用且简单");
        Console.WriteLine("  - 库代码：性能敏感场景考虑 ValueTask<T>");
        Console.WriteLine("  - 有疑问时：先用 Task<T>，性能问题再优化\n");
    }
}
