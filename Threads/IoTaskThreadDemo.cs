// ========================================
// I/O Task 不占用线程验证
// ========================================

namespace Threads;

/// <summary>
/// 验证 I/O 密集型 Task 不占用线程
/// </summary>
internal static class IoTaskThreadDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("验证：10000 个 I/O 密集型 Task 不会占用 10000 个线程\n");

        ThreadPool.GetMinThreads(out int minWorker, out int minIO);
        ThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);

        Console.WriteLine("线程池配置：");
        Console.WriteLine($"  最小工作线程：{minWorker}");
        Console.WriteLine($"  最大工作线程：{maxWorker}");
        Console.WriteLine($"  初始线程数：{ThreadPool.ThreadCount}\n");

        const int taskCount = 10000;

        Console.WriteLine($"创建 {taskCount} 个 I/O 密集型 Task...");

        var initialThreadCount = ThreadPool.ThreadCount;
        var tasks = new Task[taskCount];

        for (int i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                // I/O 密集型：使用 Task.Delay（不占用线程）
                await Task.Delay(5000);
            });
        }

        // 等待任务启动
        await Task.Delay(200);

        var runningThreadCount = ThreadPool.ThreadCount;
        ThreadPool.GetAvailableThreads(out int availWorker, out _);

        Console.WriteLine($"\n任务运行中：");
        Console.WriteLine($"  Task 数量：{taskCount:N0}");
        Console.WriteLine($"  实际线程数：{runningThreadCount}（初始：{initialThreadCount}）");
        Console.WriteLine($"  可用线程数：{availWorker}");
        Console.WriteLine($"  忙碌线程数：约 {maxWorker - availWorker}");

        Console.WriteLine($"\n关键结论：");
        Console.WriteLine($"  ✓ {taskCount:N0} 个 Task 只用了约 {runningThreadCount} 个线程");
        Console.WriteLine($"  ✓ 线程减少：约 {taskCount / Math.Max(runningThreadCount, 1):N0}x");
        Console.WriteLine($"  ✓ 原因：await Task.Delay 使用 OS 定时器，不占用线程");

        Console.WriteLine($"\n等待所有任务完成...");
        await Task.WhenAll(tasks);

        var finalThreadCount = ThreadPool.ThreadCount;
        Console.WriteLine($"所有任务完成，最终线程数：{finalThreadCount}");

        Console.WriteLine();
        Console.WriteLine("总结：");
        Console.WriteLine("  I/O 密集型操作（如 Task.Delay、网络请求、文件读写）");
        Console.WriteLine("  在等待期间会释放线程，不占用线程池资源");
        Console.WriteLine("  这就是 async/await 的核心优势！");
    }

    /// <summary>
    /// 对比：CPU 密集型 Task 会占用线程
    /// </summary>
    public static async Task CompareCpuBoundTaskAsync()
    {
        Console.WriteLine("\n=== 对比：CPU 密集型 Task ===");

        const int taskCount = 100;  // 减少数量，避免耗尽线程池

        Console.WriteLine($"创建 {taskCount} 个 CPU 密集型 Task...");

        var initialThreadCount = ThreadPool.ThreadCount;
        var tasks = new Task[taskCount];

        for (int i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                // CPU 密集型：占用线程进行计算
                for (int j = 0; j < 1_000_000; j++)
                {
                    var result = Math.Sqrt(j);
                }
            });
        }

        await Task.Delay(100);

        var runningThreadCount = ThreadPool.ThreadCount;

        Console.WriteLine($"  Task 数量：{taskCount}");
        Console.WriteLine($"  实际线程数：{runningThreadCount}（初始：{initialThreadCount}）");
        Console.WriteLine($"  说明：CPU 密集型 Task 会占用线程池线程");

        await Task.WhenAll(tasks);
    }
}
