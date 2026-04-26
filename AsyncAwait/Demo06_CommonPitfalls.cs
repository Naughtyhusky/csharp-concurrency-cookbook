namespace AsyncAwait;

/// <summary>
/// Demo06: 常见陷阱演示
/// </summary>
public static class Demo06_CommonPitfalls
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== Demo06: 常见陷阱 ===\n");

        // 6.1 async void 陷阱
        Console.WriteLine("--- 6.1 async void 的危险 ---");
        AsyncVoidPitfall();
        await Task.Delay(2000); // 等待 async void 完成

        // 6.2 死锁演示（在控制台应用中不会死锁，仅演示概念）
        Console.WriteLine("\n--- 6.2 死锁场景（概念演示）---");
        await DeadlockScenario();

        // 6.3 过度异步化
        Console.WriteLine("\n--- 6.3 过度异步化 ---");
        await OverAsyncPitfall();

        // 6.4 忘记 await
        Console.WriteLine("\n--- 6.4 忘记 await（火-and-forget）---");
        await ForgetAwaitPitfall();
    }

    // 陷阱 1：async void
    static void AsyncVoidPitfall()
    {
        Console.WriteLine("✓ 测试 async void:");

        try
        {
            // ❌ async void 方法
            DangerousAsyncVoid();

            Console.WriteLine("  ⚠️ 调用方继续执行，无法知道异步操作何时完成");
            Console.WriteLine("  ⚠️ 如果异步方法抛出异常，应用会崩溃！");
        }
        catch (Exception ex)
        {
            // 永远不会捕获到异常
            Console.WriteLine($"  ❌ 捕获异常: {ex.Message}");
        }

        // ✅ 正确：async Task
        Console.WriteLine("\n✓ 正确做法: async Task");
        SafeAsyncTask().Wait(); // 可以等待和捕获异常
    }

    static async void DangerousAsyncVoid()
    {
        await Task.Delay(500);
        Console.WriteLine("  ⚠️ async void 方法完成");
        // throw new Exception("这个异常会导致应用崩溃！"); // 取消注释查看效果
    }

    static async Task SafeAsyncTask()
    {
        await Task.Delay(500);
        Console.WriteLine("  ✅ async Task 方法完成");
    }

    // 陷阱 2：死锁（在控制台应用中不会发生，但概念重要）
    static async Task DeadlockScenario()
    {
        Console.WriteLine("✓ 死锁场景说明（在 UI 或 ASP.NET Framework 中会发生）：");
        Console.WriteLine("  1. 主线程调用 AsyncMethod().Result");
        Console.WriteLine("  2. AsyncMethod 捕获了 SynchronizationContext");
        Console.WriteLine("  3. await 完成后，尝试回到主线程");
        Console.WriteLine("  4. 但主线程被 .Result 阻塞");
        Console.WriteLine("  5. 死锁！");

        Console.WriteLine("\n✓ 演示（在控制台中不会死锁）：");
        var result = await GetDataAsync();
        Console.WriteLine($"  ✅ 结果: {result}");

        Console.WriteLine("\n  💡 避免死锁的方法：");
        Console.WriteLine("    1. 一路 async 到底（不使用 .Result/.Wait）");
        Console.WriteLine("    2. 使用 ConfigureAwait(false)");
    }

    static async Task<string> GetDataAsync()
    {
        await Task.Delay(100);
        return "Data";
    }

    // 陷阱 3：过度异步化
    static async Task OverAsyncPitfall()
    {
        // ❌ 错误：为同步操作添加 async
        Console.WriteLine("❌ 错误：为同步操作添加 async");
        var sw1 = System.Diagnostics.Stopwatch.StartNew();
        int result1 = await UnnecessaryAsyncAdd(1, 2);
        sw1.Stop();
        Console.WriteLine($"  结果: {result1}, 耗时: {sw1.Elapsed.TotalMicroseconds:F2} μs");

        // ✅ 正确：同步方法
        Console.WriteLine("\n✅ 正确：同步方法");
        var sw2 = System.Diagnostics.Stopwatch.StartNew();
        int result2 = Add(1, 2);
        sw2.Stop();
        Console.WriteLine($"  结果: {result2}, 耗时: {sw2.Elapsed.TotalMicroseconds:F2} μs");

        // ❌ 错误：Task.Run 包装同步方法
        Console.WriteLine("\n❌ 错误：Task.Run 包装同步方法（浪费线程）");
        await Task.Run(() =>
        {
            int result = Add(3, 4);
            Console.WriteLine($"  结果: {result}（浪费了一个线程池线程）");
        });
    }

    static async Task<int> UnnecessaryAsyncAdd(int a, int b)
    {
        return a + b; // 编译器警告：CS1998
    }

    static int Add(int a, int b)
    {
        return a + b;
    }

    // 陷阱 4：忘记 await
    static async Task ForgetAwaitPitfall()
    {
        Console.WriteLine("❌ 错误：忘记 await（fire-and-forget）");

        // 启动但不等待
        DoWorkAsync(); // ⚠️ 异常会被吞掉

        Console.WriteLine("  ⚠️ 方法已返回，但异步操作可能还在执行");
        Console.WriteLine("  ⚠️ 如果异步操作抛出异常，会被吞掉");

        await Task.Delay(1000); // 等待一下，让异步操作有机会完成

        Console.WriteLine("\n✅ 正确做法 1：await 等待");
        try
        {
            await DoWorkAsync();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✅ 捕获异常: {ex.Message}");
        }

        Console.WriteLine("\n✅ 正确做法 2：明确的 fire-and-forget");
        _ = DoWorkSafelyAsync(); // 使用 discard
        await Task.Delay(1000);
    }

    static async Task DoWorkAsync()
    {
        await Task.Delay(200);
        throw new Exception("异步操作失败");
    }

    static async Task DoWorkSafelyAsync()
    {
        try
        {
            await Task.Delay(200);
            Console.WriteLine("  ✅ 异步操作完成（fire-and-forget，但有异常处理）");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ✅ 记录异常: {ex.Message}");
        }
    }
}
