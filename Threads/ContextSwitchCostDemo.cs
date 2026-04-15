// ========================================
// Thread 上下文切换成本演示
// ========================================

using System.Diagnostics;

namespace Threads;

/// <summary>
/// 演示线程上下文切换的成本
/// </summary>
internal static class ContextSwitchCostDemo
{
    public static void Run()
    {
        const int iterations = 1_000_000;

        Console.WriteLine($"测试迭代次数：{iterations:N0}\n");

        // 基准测试：单线程
        MeasureSingleThread(iterations);

        Console.WriteLine();

        // 对比测试：多线程（频繁上下文切换）
        MeasureMultiThreadWithContextSwitch(iterations);

        Console.WriteLine();

        // 对比测试：多线程（无上下文切换）
        MeasureMultiThreadWithoutContextSwitch(iterations);
    }

    /// <summary>
    /// 单线程基准测试
    /// </summary>
    private static void MeasureSingleThread(int iterations)
    {
        Console.WriteLine("=== 单线程基准测试 ===");

        var sw = Stopwatch.StartNew();

        for (int i = 0; i < iterations; i++)
        {
            // 简单计算
            var result = i * i;
        }

        sw.Stop();

        Console.WriteLine($"  耗时：{sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  说明：无上下文切换开销");
    }

    /// <summary>
    /// 多线程（强制上下文切换）
    /// </summary>
    private static void MeasureMultiThreadWithContextSwitch(int iterations)
    {
        Console.WriteLine("=== 多线程（频繁上下文切换）===");

        const int threadCount = 10;
        var threads = new Thread[threadCount];
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < iterations / threadCount; j++)
                {
                    // 简单计算
                    var result = j * j;

                    // 强制上下文切换
                    Thread.Sleep(0);
                }
            });
            threads[i].Start();
        }

        foreach (var t in threads)
        {
            t.Join();
        }

        sw.Stop();

        Console.WriteLine($"  线程数：{threadCount}");
        Console.WriteLine($"  耗时：{sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  说明：Thread.Sleep(0) 强制上下文切换，开销巨大");
    }

    /// <summary>
    /// 多线程（无强制上下文切换）
    /// </summary>
    private static void MeasureMultiThreadWithoutContextSwitch(int iterations)
    {
        Console.WriteLine("=== 多线程（无强制上下文切换）===");

        const int threadCount = 10;
        var threads = new Thread[threadCount];
        var sw = Stopwatch.StartNew();

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                for (int j = 0; j < iterations / threadCount; j++)
                {
                    // 简单计算
                    var result = j * j;
                    // 不强制上下文切换
                }
            });
            threads[i].Start();
        }

        foreach (var t in threads)
        {
            t.Join();
        }

        sw.Stop();

        Console.WriteLine($"  线程数：{threadCount}");
        Console.WriteLine($"  耗时：{sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  说明：正常多线程，上下文切换由 OS 调度");
    }

    /// <summary>
    /// 演示线程创建的成本
    /// </summary>
    public static void MeasureThreadCreationCost()
    {
        Console.WriteLine("\n=== 线程创建成本测试 ===");

        const int count = 1000;

        // 测试 Thread 创建
        var sw1 = Stopwatch.StartNew();
        for (int i = 0; i < count; i++)
        {
            var thread = new Thread(() => { });
            thread.Start();
            thread.Join();
        }
        sw1.Stop();
        Console.WriteLine($"创建 {count} 个 Thread：{sw1.ElapsedMilliseconds}ms");

        // 测试 Task 创建（使用 ThreadPool）
        var sw2 = Stopwatch.StartNew();
        var tasks = new Task[count];
        for (int i = 0; i < count; i++)
        {
            tasks[i] = Task.Run(() => { });
        }
        Task.WaitAll(tasks);
        sw2.Stop();
        Console.WriteLine($"创建 {count} 个 Task：{sw2.ElapsedMilliseconds}ms");

        Console.WriteLine($"性能提升：{sw1.ElapsedMilliseconds / (double)sw2.ElapsedMilliseconds:F1}x");
    }
}
