namespace ProductionDiagnostics;

/// <summary>
/// 线程池饥饿演示：模拟线程池线程被耗尽的场景
/// 诊断工具：dotnet-counters
/// </summary>
public static class ThreadPoolStarvationDemo
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n=== 线程池饥饿演示 ===");
        Console.WriteLine("模拟场景：所有线程池线程都被阻塞，导致新任务无法执行\n");
        Console.WriteLine("⚠️  建议在另一个终端使用 dotnet-counters 监控：");
        Console.WriteLine("dotnet-counters monitor -p <进程ID> --counters System.Runtime[threadpool-thread-count,threadpool-queue-length]\n");

        Console.WriteLine("按任意键开始演示...");
        Console.ReadKey();
        Console.WriteLine();

        // 获取线程池配置
        ThreadPool.GetMinThreads(out int minWorker, out int minIO);
        ThreadPool.GetMaxThreads(out int maxWorker, out int maxIO);
        Console.WriteLine($"线程池配置：最小线程数={minWorker}，最大线程数={maxWorker}\n");

        // 场景1：同步阻塞导致线程池饥饿
        Console.WriteLine("场景1：用 Task.Run 执行同步阻塞操作（错误做法）");
        Console.WriteLine($"将启动 {maxWorker} 个任务，每个任务阻塞 10 秒...\n");

        var stopwatch = System.Diagnostics.Stopwatch.StartNew();

        // 创建大量阻塞任务（模拟错误的异步实现）
        var tasks = new List<Task>();
        for (int i = 0; i < 500; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(() =>
            {
                Console.WriteLine($"[Task {taskId}] 线程 {Environment.CurrentManagedThreadId} 开始执行");
                Thread.Sleep(10000); // 阻塞线程池线程（错误做法）
                Console.WriteLine($"[Task {taskId}] 线程 {Environment.CurrentManagedThreadId} 完成");
            }));

            // 每启动10个任务，暂停一下观察
            if ((i + 1) % 10 == 0)
            {
                Console.WriteLine($"已提交 {i + 1} 个任务...");
                await Task.Delay(100);
            }
        }

        Console.WriteLine("\n所有任务已提交，等待完成...");
        Console.WriteLine("💡 观察现象：前几个任务立即执行，后面的任务排队等待（线程池饥饿）\n");

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"\n✅ 所有任务完成，总耗时: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("\n❌ 问题分析：");
        Console.WriteLine("- Thread.Sleep 阻塞了线程池线程");
        Console.WriteLine("- 线程池注入新线程的速度有限（每秒约2个）");
        Console.WriteLine("- 导致后面的任务排队等待\n");

        Console.WriteLine("✅ 正确做法：使用异步方法（按任意键查看对比）");
        Console.ReadKey();
        Console.WriteLine();

        // 场景2：正确的异步实现
        Console.WriteLine("\n场景2：使用异步方法（正确做法）");
        Console.WriteLine("将启动 500 个异步任务，每个任务等待 10 秒...\n");

        stopwatch.Restart();
        tasks.Clear();

        for (int i = 0; i < 500; i++)
        {
            int taskId = i;
            tasks.Add(Task.Run(async () =>
            {
                Console.WriteLine($"[Task {taskId}] 线程 {Environment.CurrentManagedThreadId} 开始执行");
                await Task.Delay(10000); // 异步等待，不阻塞线程
                Console.WriteLine($"[Task {taskId}] 线程 {Environment.CurrentManagedThreadId} 完成");
            }));

            if ((i + 1) % 10 == 0)
            {
                Console.WriteLine($"已提交 {i + 1} 个任务...");
                await Task.Delay(100);
            }
        }

        Console.WriteLine("\n所有任务已提交，等待完成...");
        Console.WriteLine("💡 观察现象：所有任务几乎同时开始执行（无线程池饥饿）\n");

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        Console.WriteLine($"\n✅ 所有任务完成，总耗时: {stopwatch.ElapsedMilliseconds}ms");
        Console.WriteLine("\n✅ 优化效果：");
        Console.WriteLine("- await Task.Delay 不阻塞线程");
        Console.WriteLine("- 线程在等待期间被释放，可以处理其他任务");
        Console.WriteLine("- 所有任务几乎同时完成\n");

        Console.WriteLine("💡 总结：避免线程池饥饿的关键：");
        Console.WriteLine("1. 不要在 Task.Run 中使用同步阻塞方法（Thread.Sleep、.Wait()、.Result）");
        Console.WriteLine("2. 使用异步方法（async/await、Task.Delay）");
        Console.WriteLine("3. I/O 操作使用异步 API（ReadAsync、WriteAsync、SendAsync）");
        Console.WriteLine("4. 如果必须使用同步阻塞，考虑创建专用线程或增加线程池最小线程数");
    }
}
