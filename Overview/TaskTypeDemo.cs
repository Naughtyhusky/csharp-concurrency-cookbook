// ========================================
// 任务类型识别示例
// ========================================

using System.Diagnostics;

namespace Overview;

/// <summary>
/// 任务类型识别：CPU 密集型、I/O 密集型、混合型
/// </summary>
internal static class TaskTypeDemo
{
    /// <summary>
    /// 运行任务类型识别示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("【任务类型识别演示】\n");

        // CPU 密集型
        Console.WriteLine("--- 1. CPU 密集型：大数据筛选 ---");
        DemonstrateCpuBoundTask();

        // I/O 密集型
        Console.WriteLine("\n--- 2. I/O 密集型：模拟 API 调用 ---");
        await DemonstrateIoBoundTaskAsync();

        // 混合型
        Console.WriteLine("\n--- 3. 混合型：下载并处理数据 ---");
        await DemonstrateMixedTaskAsync();

        Console.WriteLine();
    }

    /// <summary>
    /// CPU 密集型任务：使用 PLINQ 查找质数
    /// </summary>
    private static void DemonstrateCpuBoundTask()
    {
        var data = Enumerable.Range(1, 1_000_000).ToArray();
        var sw = Stopwatch.StartNew();

        // 使用 PLINQ 并行处理
        var primes = data
            .AsParallel()
            .Where(IsPrime)
            .ToArray();

        sw.Stop();
        Console.WriteLine($"找到 {primes.Length} 个质数，耗时: {sw.ElapsedMilliseconds}ms");
    }

    private static bool IsPrime(int number)
    {
        if (number < 2) return false;
        for (int i = 2; i <= Math.Sqrt(number); i++)
        {
            if (number % i == 0) return false;
        }
        return true;
    }

    /// <summary>
    /// I/O 密集型任务：并发调用多个 API
    /// </summary>
    private static async Task DemonstrateIoBoundTaskAsync()
    {
        var urls = new[] { "api1", "api2", "api3" };
        var sw = Stopwatch.StartNew();

        // 并发调用多个 API
        var tasks = urls.Select(url => FetchDataAsync(url));
        var results = await Task.WhenAll(tasks);

        sw.Stop();
        Console.WriteLine($"完成 {results.Length} 个 API 调用，总耗时: {sw.ElapsedMilliseconds}ms");
    }

    private static async Task<string> FetchDataAsync(string url)
    {
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 开始调用 {url}");
        await Task.Delay(500); // 模拟网络延迟
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] {url} 完成 ✓");
        return $"Data from {url}";
    }

    /// <summary>
    /// 混合型任务：下载 → 处理 → 保存
    /// </summary>
    private static async Task DemonstrateMixedTaskAsync()
    {
        var sw = Stopwatch.StartNew();

        // 步骤1：I/O 密集 - 下载数据
        var rawData = await DownloadDataAsync();

        // 步骤2：CPU 密集 - 处理数据（使用 Task.Run）
        var processed = await Task.Run(() => ProcessDataCpu(rawData));

        // 步骤3：I/O 密集 - 保存结果
        await SaveDataAsync(processed);

        sw.Stop();
        Console.WriteLine($"混合型任务完成，总耗时: {sw.ElapsedMilliseconds}ms");
    }

    private static async Task<int[]> DownloadDataAsync()
    {
        Console.WriteLine("  下载数据中...");
        await Task.Delay(500);
        Console.WriteLine("  下载完成 ✓");
        return Enumerable.Range(1, 10000).ToArray();
    }

    private static int[] ProcessDataCpu(int[] data)
    {
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 处理数据中（CPU 密集）...");
        Thread.Sleep(800); // 模拟 CPU 计算
        Console.WriteLine($"  [Thread {Environment.CurrentManagedThreadId}] 处理完成 ✓");
        return data.Where(x => x % 2 == 0).ToArray();
    }

    private static async Task SaveDataAsync(int[] data)
    {
        Console.WriteLine("  保存数据中...");
        await Task.Delay(300);
        Console.WriteLine($"  保存完成，共 {data.Length} 条记录 ✓");
    }
}
