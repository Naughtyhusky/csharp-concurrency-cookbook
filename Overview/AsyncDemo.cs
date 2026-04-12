// ========================================
// 异步（Asynchronous）示例
// ========================================

using System.Diagnostics;

namespace Overview;

/// <summary>
/// 异步示例：不阻塞线程，适合 I/O 密集型操作
/// </summary>
internal static class AsyncDemo
{
    /// <summary>
    /// 运行异步示例
    /// </summary>
    public static async Task RunAsync()
    {
        Console.WriteLine("【3. 异步示例：I/O 密集型操作】");
        Console.WriteLine("说明：异步操作在等待期间不占用线程\n");

        // 示例 1：单个异步操作（观察线程行为）
        Console.WriteLine("=== 示例 1：单个异步操作（观察线程行为） ===");
        await SimulateDownloadAsync("文件A", 1000);
        Console.WriteLine();

        // 示例 2：多个并发异步操作（展示异步的价值）
        Console.WriteLine("=== 示例 2：并发下载多个文件 ===");
        await DownloadMultipleFilesAsync();
        Console.WriteLine();

        // 示例 3：对比同步 vs 异步（性能差异）
        Console.WriteLine("=== 示例 3：同步 vs 异步性能对比 ===");
        await ComparePerformanceAsync();
        Console.WriteLine();

        // 示例 4：强制线程切换（理解 ConfigureAwait）
        Console.WriteLine("=== 示例 4：强制线程切换 ===");
        await DemonstrateThreadSwitchingAsync();

        Console.WriteLine();
    }

    /// <summary>
    /// 模拟异步下载单个文件
    /// </summary>
    private static async Task SimulateDownloadAsync(string fileName, int delayMs)
    {
        var startThread = Environment.CurrentManagedThreadId;
        Console.WriteLine($"  [Thread {startThread}] 开始下载 {fileName}...");

        // 模拟异步 I/O 操作（等待期间线程被释放）
        await Task.Delay(delayMs);

        var endThread = Environment.CurrentManagedThreadId;
        Console.WriteLine($"  [Thread {endThread}] {fileName} 下载完成 ✓");

        // 注意：控制台应用中，await 后可能在同一线程恢复（线程池优化）
        // 在 ASP.NET Core 中，通常会在不同线程恢复
        if (startThread != endThread)
        {
            Console.WriteLine($"  → 线程切换：{startThread} → {endThread}");
        }
        else
        {
            Console.WriteLine($"  → 线程复用：线程池优化，复用了 Thread {startThread}");
        }
    }

    /// <summary>
    /// 并发下载多个文件（展示异步的并发能力）
    /// </summary>
    private static async Task DownloadMultipleFilesAsync()
    {
        var sw = Stopwatch.StartNew();

        // 并发启动多个下载任务
        var task1 = SimulateDownloadAsync("文件1.zip", 800);
        var task2 = SimulateDownloadAsync("文件2.zip", 1000);
        var task3 = SimulateDownloadAsync("文件3.zip", 600);

        // 等待所有任务完成
        await Task.WhenAll(task1, task2, task3);

        sw.Stop();
        Console.WriteLine($"  总耗时: {sw.ElapsedMilliseconds}ms（如果串行需要 {800 + 1000 + 600}ms）");
    }

    /// <summary>
    /// 对比同步和异步的性能差异
    /// </summary>
    private static async Task ComparePerformanceAsync()
    {
        // 同步方式（阻塞线程）
        Console.WriteLine("  同步方式（Thread.Sleep）：");
        var sw1 = Stopwatch.StartNew();
        SimulateDownloadSync("文件A", 500);
        SimulateDownloadSync("文件B", 500);
        sw1.Stop();
        Console.WriteLine($"  → 总耗时: {sw1.ElapsedMilliseconds}ms，线程被阻塞\n");

        // 异步方式（不阻塞线程）
        Console.WriteLine("  异步方式（await Task.Delay）：");
        var sw2 = Stopwatch.StartNew();
        await SimulateDownloadAsync("文件A", 500);
        await SimulateDownloadAsync("文件B", 500);
        sw2.Stop();
        Console.WriteLine($"  → 总耗时: {sw2.ElapsedMilliseconds}ms");
        Console.WriteLine($"  → 关键差异：异步等待期间，线程被释放，可以处理其他请求！");
        Console.WriteLine($"  → 注意：虽然可能在同一线程完成，但等待期间线程是空闲的");
    }

    /// <summary>
    /// 同步下载（阻塞线程）
    /// </summary>
    private static void SimulateDownloadSync(string fileName, int delayMs)
    {
        var thread = Environment.CurrentManagedThreadId;
        Console.WriteLine($"    [Thread {thread}] 开始下载 {fileName}（阻塞中...）");
        Thread.Sleep(delayMs); // 阻塞线程
        Console.WriteLine($"    [Thread {thread}] {fileName} 下载完成");
    }

    /// <summary>
    /// 演示如何强制线程切换（理解线程池行为）
    /// </summary>
    private static async Task DemonstrateThreadSwitchingAsync()
    {
        Console.WriteLine("  说明：通过并发操作让线程池更忙碌，增加线程切换概率\n");

        var startThread = Environment.CurrentManagedThreadId;
        Console.WriteLine($"  [Thread {startThread}] 主线程开始");

        // 创建多个并发任务，让线程池更忙碌
        var tasks = new List<Task>();
        for (int i = 0; i < 10; i++)
        {
            int taskNum = i;
            tasks.Add(Task.Run(async () =>
            {
                var tid = Environment.CurrentManagedThreadId;
                Console.WriteLine($"    [Thread {tid}] 后台任务 {taskNum} 执行中...");
                await Task.Delay(100);
            }));
        }

        // 在繁忙的线程池中执行异步操作
        await Task.Delay(50);
        var midThread = Environment.CurrentManagedThreadId;
        Console.WriteLine($"\n  [Thread {midThread}] await 后恢复执行");

        await Task.WhenAll(tasks);
        var endThread = Environment.CurrentManagedThreadId;
        Console.WriteLine($"\n  [Thread {endThread}] 所有任务完成");

        Console.WriteLine($"\n  → 线程变化：{startThread} → {midThread} → {endThread}");
        Console.WriteLine($"  → 关键点：线程池会根据负载动态分配线程");
    }
}
