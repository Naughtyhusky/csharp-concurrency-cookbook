namespace Parallel_PLinq.PLinqBasic;

/// <summary>
/// PLINQ（Parallel LINQ）基本用法演示
/// AsParallel、WithDegreeOfParallelism、AsOrdered、ForAll
/// </summary>
public static class PLinqDemo
{
    private static readonly int[] LargeArray = [.. Enumerable.Range(1, 5_000_000)];

    // ─── 1. AsParallel 基本用法 ───────────────────────────────────────────────

    /// <summary>
    /// 最简单的并行 LINQ：只需加一个 .AsParallel()
    /// PLINQ 会自动把数据分区，分配到多核上执行
    /// </summary>
    public static void BasicAsParallelDemo()
    {
        Console.WriteLine("\n── AsParallel() 基本用法 ──");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 普通 LINQ（单线程）
        var seqResult = LargeArray
            .Where(x => IsPrime(x))
            .Count();
        sw.Stop();
        Console.WriteLine($"  顺序 LINQ：{seqResult:N0} 个素数，耗时 {sw.ElapsedMilliseconds} ms");

        sw.Restart();

        // 并行 LINQ（多线程，只差一个 .AsParallel()）
        var parallelResult = LargeArray
            .AsParallel()
            .Where(x => IsPrime(x))
            .Count();
        sw.Stop();
        Console.WriteLine($"  并行 LINQ：{parallelResult:N0} 个素数，耗时 {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  CPU 核心数：{Environment.ProcessorCount}");
    }

    // ─── 2. WithDegreeOfParallelism：控制并发度 ───────────────────────────────

    /// <summary>
    /// 不想让 PLINQ 跑满所有核心？用 WithDegreeOfParallelism 限制
    /// 生产环境中，留一些核心给其他服务是很重要的
    /// </summary>
    public static void DegreeOfParallelismDemo()
    {
        Console.WriteLine("\n── WithDegreeOfParallelism 控制并发度 ──");

        var data = Enumerable.Range(1, 1_000_000).ToArray();

        foreach (int degree in new[] { 1, 2, 4, Environment.ProcessorCount })
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var count = data
                .AsParallel()
                .WithDegreeOfParallelism(degree)
                .Where(x => IsPrime(x))
                .Count();
            sw.Stop();
            Console.WriteLine($"  并发度={degree,2}：{count:N0} 个素数，耗时 {sw.ElapsedMilliseconds,4} ms");
        }

        Console.WriteLine($"  建议：不要超过 Environment.ProcessorCount（当前={Environment.ProcessorCount}）");
    }

    // ─── 3. AsOrdered：保持原始顺序 ──────────────────────────────────────────

    /// <summary>
    /// PLINQ 默认不保证结果顺序，如果你需要顺序，加 AsOrdered()
    /// 但顺序保持有性能代价！
    /// </summary>
    public static void AsOrderedDemo()
    {
        Console.WriteLine("\n── AsOrdered() 保持顺序 ──");

        var data = Enumerable.Range(1, 20).ToArray();

        // 无序（默认）：结果顺序不可预测
        var unordered = data
            .AsParallel()
            .Where(x => x % 2 == 0)
            .Select(x => x)
            .ToList();

        Console.Write("  无序结果: ");
        Console.WriteLine(string.Join(", ", unordered));

        // 有序：保证与原始序列顺序一致
        var ordered = data
            .AsParallel()
            .AsOrdered()
            .Where(x => x % 2 == 0)
            .Select(x => x)
            .ToList();

        Console.Write("  有序结果: ");
        Console.WriteLine(string.Join(", ", ordered));
        Console.WriteLine("  ⚠️  AsOrdered() 会引入额外的排序开销，非必要不要用");
    }

    // ─── 4. ForAll：并行副作用操作 ────────────────────────────────────────────

    /// <summary>
    /// ForAll 是 PLINQ 特有的：不合并结果，直接对每个元素并行执行副作用
    /// 比 ToList() 然后 foreach 更高效（省去了结果合并步骤）
    /// </summary>
    public static void ForAllDemo()
    {
        Console.WriteLine("\n── ForAll() 并行副作用操作 ──");

        var data = Enumerable.Range(1, 10).ToArray();
        var bag = new System.Collections.Concurrent.ConcurrentBag<string>();

        data.AsParallel()
            .Where(x => x % 2 == 0)
            .ForAll(x =>
            {
                // 注意：ForAll 的回调是并行的，写共享状态要线程安全
                bag.Add($"偶数 {x} 由线程 {Environment.CurrentManagedThreadId} 处理");
            });

        foreach (var item in bag.OrderBy(x => x))
            Console.WriteLine($"  {item}");

        Console.WriteLine("  ForAll 适合：写数据库、发消息等纯副作用操作，不关心结果顺序");
    }

    // ─── 5. PLINQ 异常处理 ───────────────────────────────────────────────────

    /// <summary>
    /// PLINQ 会把所有并行异常打包成 AggregateException
    /// （这和第07章讲的 Task.WhenAll 的异常处理机制一样！）
    /// </summary>
    public static void ExceptionHandlingDemo()
    {
        Console.WriteLine("\n── PLINQ 异常处理 ──");

        var data = Enumerable.Range(-5, 15).ToArray(); // 包含负数

        try
        {
            var results = data
                .AsParallel()
                .Select(x =>
                {
                    if (x < 0) throw new ArgumentException($"负数不合法: {x}");
                    return Math.Sqrt(x);
                })
                .ToList();
        }
        catch (AggregateException ae)
        {
            Console.WriteLine($"  捕获 AggregateException，包含 {ae.InnerExceptions.Count} 个异常：");
            foreach (var ex in ae.InnerExceptions.Take(3))
                Console.WriteLine($"    - {ex.Message}");
            Console.WriteLine("  💡 参考第07章的 AggregateException 拆解技巧来处理这些异常");
        }
    }

    /// <summary>简单素数判断（CPU 密集型计算，用于演示并行加速效果）</summary>
    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        if (n == 2) return true;
        if (n % 2 == 0) return false;
        for (int i = 3; i * i <= n; i += 2)
            if (n % i == 0) return false;
        return true;
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  PLINQ 基本用法演示");
        Console.WriteLine("═══════════════════════════════════════");

        BasicAsParallelDemo();
        DegreeOfParallelismDemo();
        AsOrderedDemo();
        ForAllDemo();
        ExceptionHandlingDemo();
    }
}
