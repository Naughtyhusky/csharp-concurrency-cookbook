using System.Collections.Concurrent;

namespace Parallel_PLinq.ParallelBasic;

/// <summary>
/// Parallel.For / ForEach 基本用法演示
/// 包含：基本并行循环、分区策略、线程本地变量
/// </summary>
public static class ParallelForDemo
{
    // ─── 1. Parallel.For 基本用法 ─────────────────────────────────────────────

    /// <summary>
    /// 最简单的 Parallel.For：并行计算每个元素的平方根
    /// </summary>
    public static void BasicParallelFor()
    {
        Console.WriteLine("\n── Parallel.For 基本用法 ──");

        int[] data = [.. Enumerable.Range(1, 10)];
        double[] results = new double[data.Length];

        Parallel.For(0, data.Length, i =>
        {
            // 注意：这里每个 i 都在不同线程上运行，所以不能共享局部状态
            results[i] = Math.Sqrt(data[i]);
            Console.WriteLine($"  [{Environment.CurrentManagedThreadId,2}] index={i}, sqrt({data[i]})={results[i]:F4}");
        });

        Console.WriteLine($"  全部完成，共 {results.Length} 个结果");
    }

    // ─── 2. Parallel.ForEach 基本用法 ────────────────────────────────────────

    /// <summary>
    /// Parallel.ForEach：并行处理集合元素
    /// 等价于 foreach，但每个迭代在独立线程上跑
    /// </summary>
    public static void BasicParallelForEach()
    {
        Console.WriteLine("\n── Parallel.ForEach 基本用法 ──");

        var urls = new[] { "page1", "page2", "page3", "page4", "page5" };

        // 用 ConcurrentBag 收集结果，因为多线程同时写
        var results = new ConcurrentBag<string>();

        Parallel.ForEach(urls, url =>
        {
            // 模拟网络请求（CPU 密集型用 Parallel，IO 密集型用 async/await！）
            Thread.Sleep(50); // 这里只是演示，生产中不推荐在 Parallel 里做 IO
            string result = $"[Thread {Environment.CurrentManagedThreadId}] 处理完成: {url}";
            results.Add(result);
            Console.WriteLine($"  {result}");
        });

        Console.WriteLine($"  总共处理 {results.Count} 个 URL");
    }

    // ─── 3. 线程本地变量（Thread-Local State）────────────────────────────────

    /// <summary>
    /// 线程本地变量：让每个线程维护自己的局部状态，最后合并
    /// 这是避免锁争用的关键技巧！
    /// </summary>
    public static void ThreadLocalStateDemo()
    {
        Console.WriteLine("\n── 线程本地变量（避免锁争用）──");

        const int count = 10_000_000;
        long totalSum = 0;

        // ✅ 正确写法：每个线程有自己的 localSum，最后才 Interlocked 合并
        Parallel.For(
            fromInclusive: 0,
            toExclusive: count,
            localInit: () => 0L,                   // 每个线程初始化自己的局部值
            body: (i, state, localSum) =>
            {
                return localSum + (i % 100);       // 在线程本地累加，不需要锁
            },
            localFinally: localSum =>
            {
                // 最后才用原子操作合并到全局变量，只发生少量锁争用
                Interlocked.Add(ref totalSum, localSum);
            }
        );

        Console.WriteLine($"  1000万次累加结果: {totalSum:N0}");
        Console.WriteLine("  关键：每线程各自累加，最后用 Interlocked 合并，完全无锁争用");
    }

    /// <summary>
    /// ❌ 错误示范：在 Parallel 内部直接操作共享变量（数据竞争）
    /// </summary>
    public static void WrongSharedVariableDemo()
    {
        Console.WriteLine("\n── ❌ 错误：直接操作共享变量（数据竞争）──");

        const int count = 100_000;
        long wrongSum = 0;

        // ❌ 多线程同时读写 wrongSum，结果不可预测
        Parallel.For(0, count, i =>
        {
            wrongSum += 1; // 非线程安全！结果每次都不同
        });

        Console.WriteLine($"  ❌ 错误累加结果（应为 {count:N0}）: {wrongSum:N0}（通常偏小）");
        Console.WriteLine("  原因：i++ 是[读-改-写]三步操作，多线程下会互相覆盖");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  Parallel.For / ForEach 基本用法演示");
        Console.WriteLine("═══════════════════════════════════════");

        BasicParallelFor();
        BasicParallelForEach();
        ThreadLocalStateDemo();
        WrongSharedVariableDemo();
    }
}
