namespace AsyncAwait;

/// <summary>
/// Demo01: async/await 基础
/// </summary>
public static class Demo01_AsyncBasics
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== Demo01: async/await 基础 ===\n");

        // 1.1 最简单的异步方法
        Console.WriteLine("--- 1.1 最简单的异步方法 ---");
        await SimplestAsyncMethod();

        // 1.2 带返回值的异步方法
        Console.WriteLine("\n--- 1.2 带返回值的异步方法 ---");
        int result = await AsyncMethodWithResult();
        Console.WriteLine($"✓ 返回值: {result}");

        // 1.3 多个 await
        Console.WriteLine("\n--- 1.3 多个 await ---");
        await MultipleAwaits();

        // 1.4 await 在不同位置
        Console.WriteLine("\n--- 1.4 await 在不同位置 ---");
        await AwaitAtDifferentPosition();
    }

    static async Task SimplestAsyncMethod()
    {
        Console.WriteLine("✓ 开始异步操作");
        await Task.Delay(500); // 模拟异步操作
        Console.WriteLine("✓ 异步操作完成");
    }

    static async Task<int> AsyncMethodWithResult()
    {
        Console.WriteLine("✓ 计算中...");
        await Task.Delay(300);
        return 42;
    }

    static async Task MultipleAwaits()
    {
        Console.WriteLine("✓ 第一个 await 开始");
        await Task.Delay(300);
        Console.WriteLine("✓ 第一个 await 完成");

        Console.WriteLine("✓ 第二个 await 开始");
        await Task.Delay(300);
        Console.WriteLine("✓ 第二个 await 完成");
    }

    static async Task<int> AwaitAtDifferentPosition()
    {
        // 启动任务（不阻塞）
        Task<int> task = Task.Run(() =>
        {
            Thread.Sleep(500);
            Console.WriteLine($"  ✓ Task 在线程 {Environment.CurrentManagedThreadId} 上完成");
            return 100;
        });

        Console.WriteLine($"✓ 任务已启动（当前线程: {Environment.CurrentManagedThreadId}）");
        Console.WriteLine("✓ 可以做其他事情...");

        // 现在等待任务完成
        int result = await task;
        Console.WriteLine($"✓ 任务结果: {result}（当前线程: {Environment.CurrentManagedThreadId}）");

        return result;
    }
}
