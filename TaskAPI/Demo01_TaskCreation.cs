namespace TaskAPI;

/// <summary>
/// 示例 1: 创建任务的三种方式
/// </summary>
public static class Demo01_TaskCreation
{
    public static void Run()
    {
        Console.WriteLine("\n--- 1.1 Task.Run (推荐方式) ---");
        TaskRunExample();

        Console.WriteLine("\n--- 1.2 Task.Factory.StartNew (高级控制) ---");
        TaskFactoryExample();

        Console.WriteLine("\n--- 1.3 new Task() (手动启动) ---");
        NewTaskExample();

        Console.WriteLine("\n--- 1.4 创建已完成的任务 ---");
        CompletedTaskExample();
    }

    static void TaskRunExample()
    {
        // 最简单的方式
        Task task = Task.Run(() =>
        {
            Console.WriteLine($"✓ 在线程 {Thread.CurrentThread.ManagedThreadId} 上执行");
            Thread.Sleep(500);
        });

        task.Wait();

        // 带返回值的版本
        Task<int> resultTask = Task.Run(() =>
        {
            Thread.Sleep(500);
            return 42;
        });

        int result = resultTask.Result;
        Console.WriteLine($"✓ 结果: {result}");
    }

    static void TaskFactoryExample()
    {
        // 标记为长时间运行任务
        Task longTask = Task.Factory.StartNew(() =>
        {
            Console.WriteLine($"✓ 长时间运行任务 (线程 {Thread.CurrentThread.ManagedThreadId})");
            Thread.Sleep(500);
        }, TaskCreationOptions.LongRunning);

        longTask.Wait();

        // 使用自定义调度器
        Task task = Task.Factory.StartNew(() =>
        {
            Console.WriteLine($"✓ 使用默认调度器 (线程 {Thread.CurrentThread.ManagedThreadId})");
        }, CancellationToken.None,
           TaskCreationOptions.None,
           TaskScheduler.Default);

        task.Wait();
    }

    static void NewTaskExample()
    {
        // 创建但不启动
        Task task = new Task(() =>
        {
            Console.WriteLine($"✓ 手动启动的任务 (线程 {Thread.CurrentThread.ManagedThreadId})");
            Thread.Sleep(500);
        });

        Console.WriteLine($"✓ 任务状态: {task.Status}"); // Created

        // 手动启动
        task.Start();
        Console.WriteLine($"✓ 启动后状态: {task.Status}"); // Running 或 WaitingToRun

        task.Wait();
        Console.WriteLine($"✓ 完成后状态: {task.Status}"); // RanToCompletion
    }

    static void CompletedTaskExample()
    {
        // 返回已完成的任务（带返回值）
        Task<int> completedTask = Task.FromResult(42);
        Console.WriteLine($"✓ 立即可用: {completedTask.Result}"); // 不会阻塞

        // 返回已完成的任务（无返回值）
        Task emptyTask = Task.CompletedTask;
        Console.WriteLine($"✓ 空任务已完成: {emptyTask.IsCompleted}");

        // 返回已取消的任务
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();
        Task canceledTask = Task.FromCanceled(cts.Token);
        Console.WriteLine($"✓ 已取消任务: {canceledTask.IsCanceled}");

        // 返回已失败的任务
        Task faultedTask = Task.FromException(new InvalidOperationException("模拟错误"));
        Console.WriteLine($"✓ 已失败任务: {faultedTask.IsFaulted}");
    }
}
