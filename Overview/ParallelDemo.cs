// ========================================
// 并行（Parallelism）示例
// ========================================

using System.Diagnostics;

namespace Overview;

/// <summary>
/// 并行示例：物理上真正同时执行，利用多核 CPU
/// </summary>
internal static class ParallelDemo
{
    /// <summary>
    /// 运行并行示例
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("【2. 并行示例：多核处理】");
        Console.WriteLine("说明：使用 Parallel.ForEach 并行计算阶乘，充分利用多核 CPU\n");

        ParallelProcessing();

        Console.WriteLine();
    }

    /// <summary>
    /// 并行计算阶乘
    /// </summary>
    private static void ParallelProcessing()
    {
        Console.WriteLine($"CPU 核心数: {Environment.ProcessorCount}");
        Console.WriteLine("开始并行计算阶乘...");

        var numbers = Enumerable.Range(1, 8).ToArray();
        var sw = Stopwatch.StartNew();

        // 使用 Parallel.ForEach 并行处理
        Parallel.ForEach(numbers, number =>
        {
            var threadId = Environment.CurrentManagedThreadId;
            var result = ComputeFactorial(number);
            Console.WriteLine($"  [Thread {threadId}] {number}! = {result}");
        });

        sw.Stop();
        Console.WriteLine($"并行处理完成，耗时: {sw.ElapsedMilliseconds}ms");
    }

    /// <summary>
    /// 计算阶乘（CPU 密集型操作）
    /// </summary>
    private static long ComputeFactorial(int n)
    {
        long result = 1;
        for (int i = 1; i <= n; i++)
        {
            result *= i;
        }
        Thread.Sleep(200); // 模拟耗时计算
        return result;
    }
}
