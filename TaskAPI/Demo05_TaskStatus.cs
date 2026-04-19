namespace TaskAPI;

/// <summary>
/// 示例 5: 任务状态查询
/// </summary>
public static class Demo05_TaskStatus
{
    public static void Run()
    {
        Console.WriteLine("\n--- 5.1 Status 属性 - 任务生命周期 ---");
        StatusExample();

        Console.WriteLine("\n--- 5.2 常用状态属性 ---");
        StatusPropertiesExample();

        Console.WriteLine("\n--- 5.3 轮询 vs 等待（反面教材）---");
        PollingVsWaitingExample();
    }

    static void StatusExample()
    {
        Task task = new Task(() =>
        {
            Thread.Sleep(1000);
            Console.WriteLine("  ✓ 任务执行中");
        });

        Console.WriteLine($"✓ 创建后: {task.Status}"); // Created

        task.Start();
        Console.WriteLine($"✓ 启动后: {task.Status}"); // WaitingToRun 或 Running

        // 短暂延迟，让任务开始执行
        Thread.Sleep(100);
        Console.WriteLine($"✓ 执行中: {task.Status}"); // Running

        task.Wait();
        Console.WriteLine($"✓ 完成后: {task.Status}"); // RanToCompletion

        // 演示其他状态
        Console.WriteLine("\n✓ 其他可能的状态:");

        // Canceled 状态
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.Cancel();
        Task canceledTask = Task.Run(() => { }, cts.Token);
        try { canceledTask.Wait(); } catch { }
        Console.WriteLine($"  取消的任务: {canceledTask.Status}");

        // Faulted 状态
        Task faultedTask = Task.Run(() => throw new Exception("错误"));
        try { faultedTask.Wait(); } catch { }
        Console.WriteLine($"  失败的任务: {faultedTask.Status}");
    }

    static void StatusPropertiesExample()
    {
        Console.WriteLine("⏳ 创建不同状态的任务:");

        // 成功完成的任务
        Task<int> successTask = Task.Run(() =>
        {
            Thread.Sleep(500);
            return 42;
        });
        successTask.Wait();

        Console.WriteLine($"\n✓ 成功的任务:");
        Console.WriteLine($"  IsCompleted: {successTask.IsCompleted}");
        Console.WriteLine($"  IsCompletedSuccessfully: {successTask.IsCompletedSuccessfully}");
        Console.WriteLine($"  IsFaulted: {successTask.IsFaulted}");
        Console.WriteLine($"  IsCanceled: {successTask.IsCanceled}");
        Console.WriteLine($"  Result: {successTask.Result}");

        // 失败的任务
        Task faultedTask = Task.Run(() =>
        {
            throw new InvalidOperationException("模拟错误");
        });
        try { faultedTask.Wait(); } catch { }

        Console.WriteLine($"\n⚠️ 失败的任务:");
        Console.WriteLine($"  IsCompleted: {faultedTask.IsCompleted}");
        Console.WriteLine($"  IsFaulted: {faultedTask.IsFaulted}");
        Console.WriteLine($"  Exception: {faultedTask.Exception?.InnerException?.Message}");

        // 取消的任务
        CancellationTokenSource cts = new CancellationTokenSource();
        Task canceledTask = Task.Run(() =>
        {
            cts.Token.ThrowIfCancellationRequested();
        }, cts.Token);
        cts.Cancel();
        try { canceledTask.Wait(); } catch { }

        Console.WriteLine($"\n⚠️ 取消的任务:");
        Console.WriteLine($"  IsCompleted: {canceledTask.IsCompleted}");
        Console.WriteLine($"  IsCanceled: {canceledTask.IsCanceled}");
    }

    static void PollingVsWaitingExample()
    {
        // ❌ 错误：轮询方式（浪费 CPU）
        Console.WriteLine("❌ 轮询方式（不推荐）:");
        var sw = System.Diagnostics.Stopwatch.StartNew();
        int pollCount = 0;

        Task<int> pollingTask = Task.Run(() =>
        {
            Thread.Sleep(1000);
            return 42;
        });

        while (!pollingTask.IsCompleted)
        {
            pollCount++;
            Thread.Sleep(50); // 浪费 CPU
        }

        sw.Stop();
        Console.WriteLine($"  结果: {pollingTask.Result}");
        Console.WriteLine($"  轮询次数: {pollCount}, 耗时: {sw.ElapsedMilliseconds}ms");

        // ✅ 正确：等待方式
        Console.WriteLine("\n✅ 等待方式（推荐）:");
        sw.Restart();

        Task<int> waitingTask = Task.Run(() =>
        {
            Thread.Sleep(1000);
            return 42;
        });

        waitingTask.Wait();
        sw.Stop();

        Console.WriteLine($"  结果: {waitingTask.Result}");
        Console.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"  ✓ 线程被有效阻塞，没有浪费 CPU");
    }
}
