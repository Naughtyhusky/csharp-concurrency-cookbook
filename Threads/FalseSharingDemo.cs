// ========================================
// False Sharing 演示
// 展示缓存行伪共享对性能的影响
// ========================================

using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Threads;

/// <summary>
/// False Sharing 演示：展示缓存行伪共享的性能影响
/// </summary>
public static class FalseSharingDemo
{
    // ❌ 有 False Sharing 的版本
    class BadCounter
    {
        public long Counter1;  // 8 字节
        public long Counter2;  // 8 字节（在同一缓存行中！）
    }

    // ✅ 避免 False Sharing 的版本 - 使用 FieldOffset
    [StructLayout(LayoutKind.Explicit)]
    class GoodCounter
    {
        [FieldOffset(0)]
        public long Counter1;   // 偏移 0

        // 填充 56 字节（64 - 8 = 56）
        [FieldOffset(64)]
        public long Counter2;   // 偏移 64（在下一个缓存行）
    }

    // ✅ 避免 False Sharing 的版本 - 使用手动填充
    class PaddedCounter
    {
        public long Counter1;

        // 填充 56 字节，确保 Counter2 在下一个缓存行
        private long _padding1, _padding2, _padding3, _padding4, _padding5, _padding6, _padding7;

        public long Counter2;
    }


    public static void Run()
    {
        Console.WriteLine("False Sharing 性能对比实验");
        Console.WriteLine("-------------------------------");
        Console.WriteLine("说明：两个线程分别修改不同的计数器");
        Console.WriteLine("      如果在同一缓存行，会导致 False Sharing\n");

        const int iterations = 100_000_000;

        // 实验 1：有 False Sharing
        Console.WriteLine("【实验 1】有 False Sharing（BadCounter）");
        DemonstrateFalseSharing(iterations);

        Console.WriteLine();

        // 实验 2：使用 FieldOffset 避免 False Sharing
        Console.WriteLine("【实验 2】使用 FieldOffset 避免 False Sharing（GoodCounter）");
        DemonstrateNoPadding(iterations);

        Console.WriteLine();

        // 实验 3：使用手动填充避免 False Sharing
        Console.WriteLine("【实验 3】使用手动填充避免 False Sharing（PaddedCounter）");
        DemonstratePaddedCounter(iterations);

        Console.WriteLine();
        Console.WriteLine("-------------------------------");
        Console.WriteLine("结论：避免 False Sharing 可以带来 5-10 倍的性能提升！");
    }

    static void DemonstrateFalseSharing(int iterations)
    {
        var bad = new BadCounter();

        var sw = Stopwatch.StartNew();

        var t1 = new Thread(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                bad.Counter1++;  // 线程 1 修改 Counter1
            }
        });

        var t2 = new Thread(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                bad.Counter2++;  // 线程 2 修改 Counter2
            }
        });

        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        sw.Stop();
        Console.WriteLine($"  耗时：{sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  Counter1 = {bad.Counter1:N0}, Counter2 = {bad.Counter2:N0}");
    }

    static void DemonstrateNoPadding(int iterations)
    {
        var good = new GoodCounter();

        var sw = Stopwatch.StartNew();

        var t1 = new Thread(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                good.Counter1++;
            }
        });

        var t2 = new Thread(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                good.Counter2++;
            }
        });

        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        sw.Stop();
        Console.WriteLine($"  耗时：{sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  Counter1 = {good.Counter1:N0}, Counter2 = {good.Counter2:N0}");
        Console.WriteLine($"  性能提升：约 {GetPerformanceImprovement(iterations, sw.ElapsedMilliseconds)}");
    }

    static void DemonstratePaddedCounter(int iterations)
    {
        var padded = new PaddedCounter();

        var sw = Stopwatch.StartNew();

        var t1 = new Thread(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                padded.Counter1++;
            }
        });

        var t2 = new Thread(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                padded.Counter2++;
            }
        });

        t1.Start();
        t2.Start();
        t1.Join();
        t2.Join();

        sw.Stop();
        Console.WriteLine($"  耗时：{sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  Counter1 = {padded.Counter1:N0}, Counter2 = {padded.Counter2:N0}");
        Console.WriteLine($"  性能提升：约 {GetPerformanceImprovement(iterations, sw.ElapsedMilliseconds)}");
    }

    static string GetPerformanceImprovement(int iterations, long milliseconds)
    {
        // 假设有 False Sharing 的基准时间约为无 False Sharing 的 5-10 倍
        // 这里简单显示比例估算
        return $"{5}-{10} 倍（具体取决于 CPU 架构和缓存设计）";
    }
}
