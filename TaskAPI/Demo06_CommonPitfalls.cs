namespace TaskAPI;

/// <summary>
/// 示例 6: 常见陷阱与最佳实践
/// </summary>
public static class Demo06_CommonPitfalls
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n--- 6.1 陷阱 1: 忘记 await 或 Wait ---");
        await Pitfall1_ForgottenAwait();

        Console.WriteLine("\n--- 6.2 陷阱 2: 循环中的闭包问题 ---");
        Pitfall2_ClosureInLoop();

        Console.WriteLine("\n--- 6.3 陷阱 3: 误用 Task.Run 包装异步方法 ---");
        await Pitfall3_WrongTaskRun();

        Console.WriteLine("\n--- 6.4 陷阱 4: 同步阻塞异步方法 (演示) ---");
        Pitfall4_SyncBlockAsync();
    }

    static async Task Pitfall1_ForgottenAwait()
    {
        Console.WriteLine("❌ 错误: 忘记 await，异常被吞掉:");

        // 错误示例（异常被吞掉）
        DoWorkWithoutAwait();
        await Task.Delay(100); // 等待一下，看看有没有异常

        Console.WriteLine("  ✓ 程序继续执行，异常被忽略了");

        // 正确示例
        Console.WriteLine("\n✅ 正确: 使用 await:");
        try
        {
            await DoWorkWithAwaitAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✓ 捕获到异常: {ex.Message}");
        }
    }

    static void DoWorkWithoutAwait()
    {
        // ❌ 任务启动但没有等待，异常会被忽略
        Task.Run(() =>
        {
            throw new Exception("这个异常会被吞掉");
        });
    }

    static async Task DoWorkWithAwaitAsync()
    {
        // ✅ 使用 await，异常可以被捕获
        await Task.Run(() =>
        {
            throw new Exception("这个异常可以被捕获");
        });
    }

    static void Pitfall2_ClosureInLoop()
    {
        // ❌ 错误: 闭包捕获循环变量
        Console.WriteLine("❌ 错误示例 (闭包问题):");
        var tasks1 = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            tasks1.Add(Task.Run(() => Console.WriteLine($"  i = {i}")));
        }
        Task.WaitAll(tasks1.ToArray());
        Console.WriteLine("  ⚠️ 可能所有任务都打印相同的值");

        // ✅ 正确: 捕获局部变量
        Console.WriteLine("\n✅ 正确示例 (使用局部变量):");
        var tasks2 = new List<Task>();
        for (int i = 0; i < 5; i++)
        {
            int localI = i; // 捕获局部变量
            tasks2.Add(Task.Run(() => Console.WriteLine($"  localI = {localI}")));
        }
        Task.WaitAll(tasks2.ToArray());
        Console.WriteLine("  ✓ 每个任务打印不同的值");
    }

    static async Task Pitfall3_WrongTaskRun()
    {
        // ❌ 错误: 用 Task.Run 包装已经是异步的方法
        Console.WriteLine("❌ 错误: 双重异步 (浪费线程):");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        await Task.Run(async () =>
        {
            await SimulateHttpRequestAsync();
        });

        sw.Stop();
        Console.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms (浪费了一个线程池线程)");

        // ✅ 正确: 直接使用异步方法
        Console.WriteLine("\n✅ 正确: 直接调用异步方法:");
        sw.Restart();

        await SimulateHttpRequestAsync();

        sw.Stop();
        Console.WriteLine($"  耗时: {sw.ElapsedMilliseconds}ms (没有占用额外线程)");

        Console.WriteLine("\n💡 原则:");
        Console.WriteLine("  • Task.Run 用于 CPU 密集型操作");
        Console.WriteLine("  • I/O 操作（网络、文件）本身就是异步的，无需 Task.Run");
    }

    static async Task<string> SimulateHttpRequestAsync()
    {
        // 模拟 I/O 操作（不占用线程）
        await Task.Delay(500);
        return "数据";
    }

    static void Pitfall4_SyncBlockAsync()
    {
        Console.WriteLine("⚠️ 注意: 同步阻塞异步方法可能导致死锁");
        Console.WriteLine("  (在 WPF/WinForms 中会死锁，在控制台应用中通常不会)");
        Console.WriteLine("\n  示例代码 (控制台应用不会死锁):");
        Console.WriteLine("  var result = GetDataAsync().Result; // 在 UI 应用中会死锁!");

        // 在控制台应用中演示（不会死锁，但不推荐）
        var result = GetDataForBlockingAsync().Result;
        Console.WriteLine($"  ✓ 获取到结果: {result}");

        Console.WriteLine("\n  ✅ 正确做法: 使用 async 一路到底");
        Console.WriteLine("  public async Task<string> GetDataAsync()");
        Console.WriteLine("  {");
        Console.WriteLine("      return await GetDataAsync();");
        Console.WriteLine("  }");
    }

    static async Task<string> GetDataForBlockingAsync()
    {
        await Task.Delay(500);
        return "数据";
    }
}
