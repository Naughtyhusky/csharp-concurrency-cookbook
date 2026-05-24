using System.Diagnostics;

namespace Parallel_PLinq.Performance;

/// <summary>
/// 性能对比：顺序 vs Parallel vs PLINQ
/// 分析并行开销与收益的临界点
/// </summary>
public static class PerformanceComparisonDemo
{
    /// <summary>模拟 CPU 密集型工作（判断素数）</summary>
    private static bool IsPrime(int n)
    {
        if (n < 2) return false;
        if (n == 2) return true;
        if (n % 2 == 0) return false;
        for (int i = 3; i * i <= n; i += 2)
            if (n % i == 0) return false;
        return true;
    }

    // ─── 1. 小数据量：并行反而更慢 ───────────────────────────────────────────

    /// <summary>
    /// 揭示一个让很多初学者困惑的现象：
    /// 数据量太小时，创建线程和调度的开销比计算本身还大！
    /// </summary>
    public static void SmallDatasetDemo()
    {
        Console.WriteLine("\n── 小数据量（100个元素）：并行不一定快 ──");

        var smallData = Enumerable.Range(1, 100).ToArray();

        // 顺序
        var sw = Stopwatch.StartNew();
        var seqCount = smallData.Where(IsPrime).Count();
        sw.Stop();
        long seqMs = sw.ElapsedMilliseconds;
        long seqTicks = sw.ElapsedTicks;

        // 并行
        sw.Restart();
        var parCount = smallData.AsParallel().Where(IsPrime).Count();
        sw.Stop();
        long parMs = sw.ElapsedMilliseconds;
        long parTicks = sw.ElapsedTicks;

        Console.WriteLine($"  顺序 LINQ:  {seqCount} 个素数，{seqTicks} ticks");
        Console.WriteLine($"  并行 PLINQ: {parCount} 个素数，{parTicks} ticks");
        Console.WriteLine($"  ⚠️  数据量小时，并行调度开销占主导，PLINQ 可能更慢！");
    }

    // ─── 2. 中等数据量：并行开始显现优势 ─────────────────────────────────────

    /// <summary>
    /// 随着数据量增加，并行的加速比逐渐显现
    /// </summary>
    public static void MediumDatasetDemo()
    {
        Console.WriteLine("\n── 不同数据量下的加速比对比 ──");
        Console.WriteLine($"  CPU 核心数: {Environment.ProcessorCount}");

        int[] sizes = [1_000, 10_000, 100_000, 1_000_000, 5_000_000];

        Console.WriteLine($"  {"数据量",-12} {"顺序(ms)",10} {"PLINQ(ms)",10} {"加速比",8}");
        Console.WriteLine($"  {"─────────────────────────────────────────────"}");

        foreach (int size in sizes)
        {
            var data = Enumerable.Range(1, size).ToArray();

            var sw = Stopwatch.StartNew();
            var seqCount = data.Where(IsPrime).Count();
            sw.Stop();
            long seqMs = sw.ElapsedMilliseconds;

            sw.Restart();
            var parCount = data.AsParallel().Where(IsPrime).Count();
            sw.Stop();
            long parMs = sw.ElapsedMilliseconds;

            double speedup = seqMs > 0 ? (double)seqMs / parMs : 0;
            string note = parMs < seqMs ? "✅" : "❌并行更慢";

            Console.WriteLine($"  {size,12:N0} {seqMs,10} {parMs,10} {speedup,7:F1}x  {note}");
        }

        Console.WriteLine($"\n  结论：数据量越大、计算越重，并行收益越明显");
        Console.WriteLine($"  经验值：通常超过 10 万个元素且每个元素计算较重时才值得并行");
    }

    // ─── 3. Parallel.For vs PLINQ 的选择 ─────────────────────────────────────

    /// <summary>
    /// Parallel.For 和 PLINQ 在同等场景下的对比
    /// </summary>
    public static void ParallelVsPLinqDemo()
    {
        Console.WriteLine("\n── Parallel.For vs PLINQ 对比 ──");

        const int size = 2_000_000;
        var data = Enumerable.Range(1, size).ToArray();
        int[] results1 = new int[size];
        var sw = Stopwatch.StartNew();

        // Parallel.For 写法
        Parallel.For(0, size, i =>
        {
            results1[i] = IsPrime(data[i]) ? data[i] : 0;
        });
        sw.Stop();
        Console.WriteLine($"  Parallel.For: {sw.ElapsedMilliseconds} ms");

        // PLINQ 写法
        sw.Restart();
        var results2 = data.AsParallel()
            .Select(x => IsPrime(x) ? x : 0)
            .ToArray();
        sw.Stop();
        Console.WriteLine($"  PLINQ:        {sw.ElapsedMilliseconds} ms");

        Console.WriteLine("  两者性能相近，选择依据：");
        Console.WriteLine("  • 已有 LINQ 代码 → 用 PLINQ，加一行 AsParallel() 即可");
        Console.WriteLine("  • 需要局部变量/中断控制 → 用 Parallel.For");
        Console.WriteLine("  • 需要保证顺序的输出 → 用 Parallel.For 配合预分配数组");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════");
        Console.WriteLine("  性能对比：顺序 vs Parallel vs PLINQ");
        Console.WriteLine("═══════════════════════════════════════");

        SmallDatasetDemo();
        MediumDatasetDemo();
        ParallelVsPLinqDemo();
    }
}
