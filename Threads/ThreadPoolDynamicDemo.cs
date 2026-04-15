// ========================================
// ThreadPool 动态调整演示
// ========================================

namespace Threads;

/// <summary>
/// 演示 ThreadPool 的动态调整机制（Hill Climbing 算法）
/// </summary>
internal static class ThreadPoolDynamicDemo
{
    public static void Run()
    {
        Console.WriteLine("设置最小线程数为 2，观察线程池如何动态增长...\n");

        // 设置最小线程数为 2
        ThreadPool.SetMinThreads(2, 2);
        ThreadPool.GetMinThreads(out int minWorker, out int minIO);
        ThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);

        Console.WriteLine($"配置：");
        Console.WriteLine($"  最小工作线程：{minWorker}");
        Console.WriteLine($"  最大工作线程：{maxWorker}");
        Console.WriteLine($"  初始线程数：{ThreadPool.ThreadCount}\n");

        Console.WriteLine("提交 50 个长时间运行的任务...\n");

        // 提交 50 个任务
        for (int i = 0; i < 50; i++)
        {
            int taskId = i;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                Console.WriteLine($"  [Task {taskId,2}] 开始执行，线程 {Environment.CurrentManagedThreadId}");
                Thread.Sleep(2000);  // 模拟工作
            });

            // 每提交 10 个任务，观察线程池状态
            if ((i + 1) % 10 == 0)
            {
                Thread.Sleep(100);  // 稍等一下让任务启动
                ThreadPool.GetAvailableThreads(out int availWorker, out int availIO);
                Console.WriteLine($"\n已提交 {i + 1} 个任务：");
                Console.WriteLine($"  当前线程数：{ThreadPool.ThreadCount}");
                Console.WriteLine($"  可用线程数：{availWorker}/{maxWorker}");
                Console.WriteLine($"  忙碌线程数：约 {maxWorker - availWorker}\n");
            }
        }

        // 等待所有任务完成
        Thread.Sleep(3000);

        Console.WriteLine("\n观察结果：");
        Console.WriteLine("  - 线程池从 2 个线程开始");
        Console.WriteLine("  - 随着任务增加，逐步注入新线程");
        Console.WriteLine("  - 注入速度：约每 500ms 增加 1 个（Hill Climbing 算法）");
        Console.WriteLine("  - 避免过度创建线程导致上下文切换");
    }
}
