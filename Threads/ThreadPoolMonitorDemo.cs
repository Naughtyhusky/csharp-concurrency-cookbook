// ========================================
// ThreadPool 实时监控
// ========================================

using System.Timers;

namespace Threads;

/// <summary>
/// 实时监控 ThreadPool 状态
/// </summary>
internal static class ThreadPoolMonitorDemo
{
    public static void Run()
    {
        Console.WriteLine("开始监控 ThreadPool 状态（持续 30 秒）\n");

        var monitorTimer = new System.Timers.Timer(1000);  // 每秒监控一次
        var startTime = DateTime.Now;
        var duration = TimeSpan.FromSeconds(30);

        monitorTimer.Elapsed += (s, e) =>
        {
            PrintThreadPoolStatus();

            if (DateTime.Now - startTime > duration)
            {
                monitorTimer.Stop();
                Console.WriteLine("\n监控结束");
            }
        };

        monitorTimer.Start();

        // 提交一些任务来观察线程池变化
        Console.WriteLine("提交 20 个长时间运行的任务...\n");
        for (int i = 0; i < 20; i++)
        {
            int taskId = i;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Thread.Sleep(5000);  // 模拟工作 5 秒
            });
        }

        // 等待监控完成或用户按 Enter
        Console.WriteLine("按 Enter 提前停止监控...");
        Console.ReadLine();
        monitorTimer.Stop();

        Console.WriteLine("\n最终状态：");
        PrintThreadPoolStatus();
    }

    /// <summary>
    /// 打印 ThreadPool 状态
    /// </summary>
    private static void PrintThreadPoolStatus()
    {
        ThreadPool.GetAvailableThreads(out int availWorker, out int availIO);
        ThreadPool.GetMinThreads(out int minWorker, out int minIO);
        ThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);

        var currentThreadCount = ThreadPool.ThreadCount;
        var busyWorkerThreads = maxWorker - availWorker;

        Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] " +
                         $"线程数: {currentThreadCount,3} | " +
                         $"可用: {availWorker,5}/{maxWorker,5} | " +
                         $"忙碌: {busyWorkerThreads,3} | " +
                         $"最小: {minWorker,2}");
    }

    /// <summary>
    /// 高级监控：包含挂起的工作项数量
    /// </summary>
    public static void RunAdvancedMonitoring()
    {
        Console.WriteLine("=== 高级监控 ===\n");

        var timer = new System.Timers.Timer(1000);
        timer.Elapsed += (s, e) =>
        {
            ThreadPool.GetAvailableThreads(out int availWorker, out int availIO);
            ThreadPool.GetMinThreads(out int minWorker, out int minIO);
            ThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);

            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}]");
            Console.WriteLine($"  工作线程：可用 {availWorker}/{maxWorker}，最小 {minWorker}");
            Console.WriteLine($"  I/O 线程：可用 {availIO}/{maxIO}，最小 {minIO}");
            Console.WriteLine($"  线程池线程总数：{ThreadPool.ThreadCount}");
            Console.WriteLine($"  待处理队列长度：{ThreadPool.PendingWorkItemCount}");
            Console.WriteLine();
        };

        timer.Start();

        // 提交任务
        for (int i = 0; i < 50; i++)
        {
            ThreadPool.QueueUserWorkItem(_ => Thread.Sleep(3000));
        }

        Console.ReadLine();
        timer.Stop();
    }
}
