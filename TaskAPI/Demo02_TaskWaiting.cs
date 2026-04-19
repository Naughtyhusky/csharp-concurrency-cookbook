namespace TaskAPI;

/// <summary>
/// 示例 2: 等待任务完成
/// </summary>
public static class Demo02_TaskWaiting
{
    public static void Run()
    {
        Console.WriteLine("\n--- 2.1 Wait() - 阻塞等待 ---");
        WaitExample();

        Console.WriteLine("\n--- 2.2 Wait(timeout) - 带超时的等待 ---");
        WaitWithTimeoutExample();

        Console.WriteLine("\n--- 2.3 Task.WaitAll - 等待所有任务 ---");
        WaitAllExample();

        Console.WriteLine("\n--- 2.4 Task.WaitAny - 等待任意一个 ---");
        WaitAnyExample();
    }

    static void WaitExample()
    {
        Task task = Task.Run(() =>
        {
            Thread.Sleep(1000);
            Console.WriteLine("✓ 任务完成");
        });

        Console.WriteLine("⏳ 阻塞等待中...");
        task.Wait();
        Console.WriteLine("✓ 继续执行");
    }

    static void WaitWithTimeoutExample()
    {
        Task longTask = Task.Run(() =>
        {
            Thread.Sleep(3000);
        });

        Console.WriteLine("⏳ 等待最多 1 秒...");
        bool completed = longTask.Wait(1000);

        if (completed)
        {
            Console.WriteLine("✓ 任务已完成");
        }
        else
        {
            Console.WriteLine("⚠️ 等待超时");
        }
    }

    static void WaitAllExample()
    {
        Console.WriteLine("⏳ 启动 3 个任务...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        Task task1 = Task.Run(() =>
        {
            Thread.Sleep(1000);
            Console.WriteLine($"  ✓ 任务 1 完成 (耗时: {sw.ElapsedMilliseconds}ms)");
        });

        Task task2 = Task.Run(() =>
        {
            Thread.Sleep(2000);
            Console.WriteLine($"  ✓ 任务 2 完成 (耗时: {sw.ElapsedMilliseconds}ms)");
        });

        Task task3 = Task.Run(() =>
        {
            Thread.Sleep(1500);
            Console.WriteLine($"  ✓ 任务 3 完成 (耗时: {sw.ElapsedMilliseconds}ms)");
        });

        Task.WaitAll(task1, task2, task3);
        sw.Stop();
        Console.WriteLine($"✓ 所有任务完成 (总耗时: {sw.ElapsedMilliseconds}ms)");
    }

    static void WaitAnyExample()
    {
        Console.WriteLine("⏳ 启动 2 个任务，等待任意一个完成...");

        Task<int> task1 = Task.Run(() =>
        {
            Thread.Sleep(2000);
            return 1;
        });

        Task<int> task2 = Task.Run(() =>
        {
            Thread.Sleep(1000);
            return 2;
        });

        int index = Task.WaitAny(task1, task2);
        Console.WriteLine($"✓ 任务 {index + 1} 先完成");

        // 超时模式示例
        Console.WriteLine("\n⏳ 超时模式示例...");
        Task<string> dataTask = Task.Run(async () =>
        {
            await Task.Delay(3000);
            return "数据";
        });

        Task timeoutTask = Task.Delay(1000);

        int timeoutIndex = Task.WaitAny(dataTask, timeoutTask);
        if (timeoutIndex == 0)
        {
            Console.WriteLine($"✓ 获取到数据: {dataTask.Result}");
        }
        else
        {
            Console.WriteLine("⚠️ 超时！");
        }
    }
}
