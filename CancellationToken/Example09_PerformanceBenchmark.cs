using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using System.Threading;

namespace CancurrencyCookBook.CancellationToken;

/// <summary>
/// 性能基准测试：检查 CancellationToken 的频率对性能的影响
/// 
/// 博客章节：第 06 章 - 7.9 性能基准测试
/// 
/// 测试目的：
/// 1. 验证不同检查频率的性能影响
/// 2. 提供实际数据支持最佳实践建议
/// 3. 演示如何使用 BenchmarkDotNet 进行性能测试
/// 
/// 运行方式：
/// 1. 在 Release 模式下运行（Debug 模式结果不准确）
/// 2. 在 Program.cs 中调用：BenchmarkRunner.Run<CancellationBenchmark>();
/// 3. 等待测试完成（约需要几分钟）
/// 
/// 注意事项：
/// - 必须在 Release 模式下运行
/// - 关闭其他占用 CPU 的程序
/// - 不要在虚拟机中运行（结果可能不准确）
/// </summary>
[MemoryDiagnoser]  // 显示内存分配情况
[SimpleJob(warmupCount: 3, iterationCount: 5)]  // 3 次预热，5 次正式测试
public class CancellationBenchmark
{
    private System.Threading.CancellationToken _token;
    private const int Iterations = 10_000_000;  // 1000 万次迭代

    [GlobalSetup]
    public void Setup()
    {
        // 创建一个未取消的 Token
        _token = new CancellationTokenSource().Token;
    }

    /// <summary>
    /// 基准测试：不检查 CancellationToken
    /// 这是性能基准，其他测试将与此对比
    /// </summary>
    [Benchmark(Baseline = true)]
    public int NoCheck()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            sum += i;
        }
        return sum;
    }

    /// <summary>
    /// 测试：每次迭代都检查 CancellationToken
    /// 预期：性能最差，因为检查次数最多
    /// </summary>
    [Benchmark]
    public int CheckEveryIteration()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            _token.ThrowIfCancellationRequested();  // 每次都检查
            sum += i;
        }
        return sum;
    }

    /// <summary>
    /// 测试：每 100 次迭代检查一次
    /// 适用场景：循环次数较少（1,000 - 10,000 次）
    /// </summary>
    [Benchmark]
    public int CheckEvery100()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            if (i % 100 == 0)
            {
                _token.ThrowIfCancellationRequested();  // 每 100 次检查
            }
            sum += i;
        }
        return sum;
    }

    /// <summary>
    /// 测试：每 1,000 次迭代检查一次
    /// 适用场景：循环次数中等（10,000 - 100,000 次）
    /// </summary>
    [Benchmark]
    public int CheckEvery1000()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            if (i % 1000 == 0)
            {
                _token.ThrowIfCancellationRequested();  // 每 1000 次检查
            }
            sum += i;
        }
        return sum;
    }

    /// <summary>
    /// 测试：每 10,000 次迭代检查一次
    /// 适用场景：循环次数较多（> 100,000 次）
    /// 预期：性能接近基准，响应时间约 1 秒
    /// </summary>
    [Benchmark]
    public int CheckEvery10000()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            if (i % 10000 == 0)
            {
                _token.ThrowIfCancellationRequested();  // 每 10000 次检查
            }
            sum += i;
        }
        return sum;
    }

    /// <summary>
    /// 测试：每 100,000 次迭代检查一次
    /// 适用场景：超大量循环（> 1,000,000 次）
    /// 预期：性能最接近基准，但响应时间较长
    /// </summary>
    [Benchmark]
    public int CheckEvery100000()
    {
        int sum = 0;
        for (int i = 0; i < Iterations; i++)
        {
            if (i % 100000 == 0)
            {
                _token.ThrowIfCancellationRequested();  // 每 100000 次检查
            }
            sum += i;
        }
        return sum;
    }
}

/// <summary>
/// 性能测试运行器
/// 使用方法：在 Program.cs 中调用 CancellationPerformanceRunner.Run()
/// </summary>
public static class CancellationPerformanceRunner
{
    /// <summary>
    /// 运行性能基准测试
    /// 注意：必须在 Release 模式下运行！
    /// </summary>
    public static void Run()
    {
        Console.WriteLine("=== CancellationToken 性能基准测试 ===");
        Console.WriteLine("注意：此测试必须在 Release 模式下运行才能获得准确结果！");
        Console.WriteLine();

#if DEBUG
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("⚠️  警告：当前处于 Debug 模式，测试结果可能不准确！");
        Console.WriteLine("⚠️  请切换到 Release 模式后重新运行。");
        Console.ResetColor();
        Console.WriteLine();
        Console.WriteLine("是否继续？(Y/N)");
        var key = Console.ReadKey(true);
        if (key.Key != ConsoleKey.Y)
        {
            Console.WriteLine("已取消测试");
            return;
        }
#endif

        Console.WriteLine("开始测试...");
        Console.WriteLine("测试将需要几分钟时间，请耐心等待...");
        Console.WriteLine();

        // 运行 BenchmarkDotNet 测试
        var summary = BenchmarkRunner.Run<CancellationBenchmark>();

        Console.WriteLine();
        Console.WriteLine("测试完成！");
        Console.WriteLine($"结果已保存到: {summary.ResultsDirectoryPath}");
    }

    /// <summary>
    /// 运行简单的性能对比（不使用 BenchmarkDotNet）
    /// 适合快速验证，但结果不如 BenchmarkDotNet 准确
    /// </summary>
    public static void RunSimple()
    {
        Console.WriteLine("=== CancellationToken 简单性能测试 ===");
        Console.WriteLine();

        var token = new CancellationTokenSource().Token;
        const int iterations = 10_000_000;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 测试1：不检查
        sw.Restart();
        int sum1 = 0;
        for (int i = 0; i < iterations; i++)
        {
            sum1 += i;
        }
        sw.Stop();
        var baseline = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"1. 不检查:         {baseline,8:F2} ms  (基准)");

        // 测试2：每次检查
        sw.Restart();
        int sum2 = 0;
        for (int i = 0; i < iterations; i++)
        {
            token.ThrowIfCancellationRequested();
            sum2 += i;
        }
        sw.Stop();
        var everyTime = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"2. 每次检查:       {everyTime,8:F2} ms  (慢 {everyTime / baseline:F2}x)");

        // 测试3：每 100 次检查
        sw.Restart();
        int sum3 = 0;
        for (int i = 0; i < iterations; i++)
        {
            if (i % 100 == 0)
                token.ThrowIfCancellationRequested();
            sum3 += i;
        }
        sw.Stop();
        var every100 = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"3. 每 100 次检查:  {every100,8:F2} ms  (慢 {every100 / baseline:F2}x)");

        // 测试4：每 1000 次检查
        sw.Restart();
        int sum4 = 0;
        for (int i = 0; i < iterations; i++)
        {
            if (i % 1000 == 0)
                token.ThrowIfCancellationRequested();
            sum4 += i;
        }
        sw.Stop();
        var every1000 = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"4. 每 1000 次检查: {every1000,8:F2} ms  (慢 {every1000 / baseline:F2}x)");

        // 测试5：每 10000 次检查
        sw.Restart();
        int sum5 = 0;
        for (int i = 0; i < iterations; i++)
        {
            if (i % 10000 == 0)
                token.ThrowIfCancellationRequested();
            sum5 += i;
        }
        sw.Stop();
        var every10000 = sw.Elapsed.TotalMilliseconds;
        Console.WriteLine($"5. 每 10000 次检查:{every10000,8:F2} ms  (慢 {every10000 / baseline:F2}x)");

        Console.WriteLine();
        Console.WriteLine("结论：");
        Console.WriteLine($"- 每次检查比基准慢 {everyTime / baseline:F2}x");
        Console.WriteLine($"- 每 100 次检查比基准慢 {every100 / baseline:F2}x");
        Console.WriteLine($"- 每 1000 次检查比基准慢 {every1000 / baseline:F2}x");
        Console.WriteLine($"- 每 10000 次检查比基准慢 {every10000 / baseline:F2}x");
        Console.WriteLine();
        Console.WriteLine("推荐：");
        Console.WriteLine("- 循环次数 < 1,000：每次检查（响应时间 < 1ms）");
        Console.WriteLine("- 循环次数 1,000 - 10,000：每 100 次检查（响应时间 ~10ms）");
        Console.WriteLine("- 循环次数 10,000 - 100,000：每 1,000 次检查（响应时间 ~100ms）");
        Console.WriteLine("- 循环次数 > 100,000：每 10,000 次检查（响应时间 ~1s）");
    }
}
