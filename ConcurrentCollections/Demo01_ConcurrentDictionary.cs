using System.Collections.Concurrent;

namespace ConcurrentCollections;

/// <summary>
/// 示例01：ConcurrentDictionary 的正确用法
/// </summary>
public static class Demo01_ConcurrentDictionary
{
    public static async Task RunAsync()
    {
        Console.WriteLine("=== Demo01：ConcurrentDictionary ===\n");

        await BasicUsageDemo();
        await AtomicOperationsDemo();
        await PageViewCounterDemo();
    }

    // ---------------------------------------------------------------
    // 1. 基础用法
    // ---------------------------------------------------------------
    static Task BasicUsageDemo()
    {
        Console.WriteLine("--- 1. 基础用法 ---");

        var dict = new ConcurrentDictionary<string, int>();

        // TryAdd：仅在 key 不存在时添加
        bool added = dict.TryAdd("apple", 1);
        Console.WriteLine($"TryAdd apple: {added}");        // True
        bool addedAgain = dict.TryAdd("apple", 99);
        Console.WriteLine($"TryAdd apple again: {addedAgain}"); // False，key已存在

        // GetOrAdd：不存在时添加并返回，存在时直接返回已有值
        int val = dict.GetOrAdd("banana", 5);
        Console.WriteLine($"GetOrAdd banana: {val}");       // 5

        // AddOrUpdate：存在就更新，不存在就添加
        dict.AddOrUpdate("apple",
            addValue: 1,
            updateValueFactory: (key, old) => old + 10);
        Console.WriteLine($"apple after AddOrUpdate: {dict["apple"]}"); // 11

        // TryUpdate：仅当当前值等于 comparisonValue 时才更新（CAS 语义）
        bool updated = dict.TryUpdate("apple", newValue: 100, comparisonValue: 11);
        Console.WriteLine($"TryUpdate apple (expect 11 → 100): {updated}, value={dict["apple"]}");

        // TryRemove
        dict.TryRemove("banana", out int removed);
        Console.WriteLine($"TryRemove banana: {removed}");

        Console.WriteLine();
        return Task.CompletedTask;
    }

    // ---------------------------------------------------------------
    // 2. 原子性演示：非原子复合操作 vs 原子操作
    // ---------------------------------------------------------------
    static async Task AtomicOperationsDemo()
    {
        Console.WriteLine("--- 2. 原子操作 vs 非原子复合操作 ---");

        const int threadCount = 10;
        const int iterationsPerThread = 1000;

        // ❌ 错误写法：读-改-写不是原子的（会丢失更新）
        var wrongDict = new ConcurrentDictionary<string, int>();
        wrongDict["counter"] = 0;
        var wrongTasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterationsPerThread; i++)
            {
                // 这两行之间不是原子的！
                var old = wrongDict["counter"];
                wrongDict["counter"] = old + 1;
            }
        })).ToArray();
        await Task.WhenAll(wrongTasks);
        Console.WriteLine($"❌ 非原子写法最终值（预期{threadCount * iterationsPerThread}）: {wrongDict["counter"]}");

        // ✅ 正确写法：AddOrUpdate 保证原子性
        var correctDict = new ConcurrentDictionary<string, int>();
        correctDict["counter"] = 0;
        var correctTasks = Enumerable.Range(0, threadCount).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterationsPerThread; i++)
            {
                correctDict.AddOrUpdate("counter", 1, (k, v) => v + 1);
            }
        })).ToArray();
        await Task.WhenAll(correctTasks);
        Console.WriteLine($"✅ 原子写法最终值（预期{threadCount * iterationsPerThread}）: {correctDict["counter"]}");

        Console.WriteLine();
    }

    // ---------------------------------------------------------------
    // 3. 实际场景：多线程页面访问计数器
    // ---------------------------------------------------------------
    static async Task PageViewCounterDemo()
    {
        Console.WriteLine("--- 3. 实战：多线程页面计数器 ---");

        var counters = new ConcurrentDictionary<string, long>();
        string[] pages = ["/home", "/about", "/products", "/contact"];

        // 模拟 20 个并发用户访问随机页面
        // Random 实例不是线程安全的，多线程下应使用 Random.Shared（.NET 6+ 线程安全）
        var tasks = Enumerable.Range(0, 20).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 50; i++)
            {
                string page = pages[Random.Shared.Next(pages.Length)];
                counters.AddOrUpdate(page, 1, (k, v) => v + 1);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Console.WriteLine("页面访问统计（共1000次）：");
        long total = 0;
        foreach (var (page, count) in counters.OrderByDescending(kv => kv.Value))
        {
            Console.WriteLine($"  {page,-15}: {count,4} 次");
            total += count;
        }
        Console.WriteLine($"  {"合计",-15}: {total,4} 次");
        Console.WriteLine();
    }
}
