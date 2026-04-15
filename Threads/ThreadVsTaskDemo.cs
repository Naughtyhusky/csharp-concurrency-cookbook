// ========================================
// Thread vs Task 对比实验
// ========================================

using System.Diagnostics;

namespace Threads;

/// <summary>
/// 震惊的实验：创建 10000 个线程 vs 10000 个 Task
/// </summary>
internal static class ThreadVsTaskDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("准备进行对比实验...\n");

        // 实验 1：创建 10000 个线程（警告：可能导致系统卡顿）
        Console.WriteLine("=== 实验 1：创建 10000 个线程 ===");
        Console.WriteLine("警告：这可能需要约 10GB 内存，系统可能卡顿");
        Console.WriteLine("是否继续？(y/n)");
        var confirm = Console.ReadLine();
        
        if (confirm?.ToLower() == "y")
        {
            try
            {
                CreateTenThousandThreads();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"创建线程失败：{ex.Message}");
            }
        }
        else
        {
            Console.WriteLine("已跳过线程实验");
        }

        Console.WriteLine();

        // 实验 2：创建 10000 个 Task
        Console.WriteLine("=== 实验 2：创建 10000 个 Task ===");
        await CreateTenThousandTasksAsync();

        Console.WriteLine();
    }

    /// <summary>
    /// 创建 10000 个线程
    /// </summary>
    private static void CreateTenThousandThreads()
    {
        const int threadCount = 10000;
        var sw = Stopwatch.StartNew();
        var threads = new Thread[threadCount];

        Console.WriteLine($"开始创建 {threadCount} 个线程...");

        for (int i = 0; i < threadCount; i++)
        {
            threads[i] = new Thread(() =>
            {
                Thread.Sleep(5000);  // 模拟工作 5 秒
            });
            threads[i].Start();

            // 每创建 1000 个线程输出一次进度
            if ((i + 1) % 1000 == 0)
            {
                Console.WriteLine($"  已创建 {i + 1} 个线程...");
            }
        }

        Console.WriteLine("所有线程已启动，等待完成...");

        // 等待所有线程完成
        foreach (var thread in threads)
        {
            thread.Join();
        }

        sw.Stop();

        Console.WriteLine($"✓ 完成时间：{sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"✓ 创建的线程数：{threadCount} 个");
        Console.WriteLine($"✓ 虚拟内存预留：约 {threadCount / 1024.0:F2} GB（每线程 1MB 栈空间）");
        Console.WriteLine($"✓ 实际物理内存：请查看任务管理器（约 1-2 GB，因为栈未被充分使用）");
        Console.WriteLine();
        Console.WriteLine("说明：");
        Console.WriteLine("  - Windows 为每个线程预留 1MB 虚拟地址空间");
        Console.WriteLine("  - 但只有栈被实际访问时，才分配物理内存（4KB 页）");
        Console.WriteLine("  - 当前代码仅调用 Thread.Sleep()，栈使用很少");
        Console.WriteLine("  - 若要查看虚拟内存占用，请使用 Process Explorer 工具");
    }

    /// <summary>
    /// 创建 10000 个 Task（I/O 密集型）
    /// </summary>
    private static async Task CreateTenThousandTasksAsync()
    {
        const int taskCount = 10000;
        var sw = Stopwatch.StartNew();
        var tasks = new Task[taskCount];

        Console.WriteLine($"开始创建 {taskCount} 个 Task...");

        // 记录初始线程数
        var initialThreadCount = ThreadPool.ThreadCount;

        for (int i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(async () =>
            {
                await Task.Delay(5000);  // I/O 密集型：不占用线程
            });
        }

        // 等待一下让任务启动
        await Task.Delay(100);

        // 查看运行时的线程数
        var runningThreadCount = ThreadPool.ThreadCount;

        Console.WriteLine("所有 Task 已启动，等待完成...");

        await Task.WhenAll(tasks);

        sw.Stop();

        Console.WriteLine($"✓ 完成时间：{sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"✓ 实际使用线程数：约 {runningThreadCount}（初始：{initialThreadCount}）");
        Console.WriteLine($"✓ Task 对象内存：约 {taskCount * 200 / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"✓ 性能优势：线程数减少约 {taskCount / Math.Max(runningThreadCount, 1)}x");

        // 对比说明
        Console.WriteLine($"\n对比分析：");
        Console.WriteLine($"  - 如果用 {taskCount} 个 Thread：内存约 {taskCount / 1024.0:F2} GB");
        Console.WriteLine($"  - 实际用 Task：内存约 {taskCount * 200 / 1024.0 / 1024.0:F2} MB");
        Console.WriteLine($"  - 内存节省：约 {(taskCount / 1024.0 * 1024) / (taskCount * 200 / 1024.0):F0}x");
    }

    /// <summary>
    /// 创建 10000 个 Task（CPU 密集型，对比）
    /// </summary>
    public static void CreateTenThousandCpuTasksAsync()
    {
        const int taskCount = 10000;
        var sw = Stopwatch.StartNew();
        var tasks = new Task[taskCount];

        Console.WriteLine($"\n=== 补充实验：创建 {taskCount} 个 CPU 密集型 Task ===");

        var initialThreadCount = ThreadPool.ThreadCount;

        for (int i = 0; i < taskCount; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                // CPU 密集型：占用线程
                for (int j = 0; j < 100000; j++)
                {
                    var result = Math.Sqrt(j);
                }
            });
        }

        Task.WaitAll(tasks);

        sw.Stop();

        var finalThreadCount = ThreadPool.ThreadCount;

        Console.WriteLine($"✓ 完成时间：{sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"✓ 线程池线程数：{initialThreadCount} → {finalThreadCount}");
        Console.WriteLine($"✓ 说明：CPU 密集型 Task 会占用线程池线程，但仍远少于 {taskCount} 个");
    }
}
