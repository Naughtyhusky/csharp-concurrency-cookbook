namespace TaskAPI;

/// <summary>
/// 示例 3: 组合任务
/// </summary>
public static class Demo03_TaskComposition
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n--- 3.1 Task.WhenAll - 并发执行 ---");
        await WhenAllExample();

        Console.WriteLine("\n--- 3.2 Task.WhenAny - 响应最快的结果 ---");
        await WhenAnyExample();

#if NET9_0_OR_GREATER
        Console.WriteLine("\n--- 3.3 Task.WhenEach - 逐个处理 (.NET 9+) ---");
        await WhenEachExample();
#else
        Console.WriteLine("\n--- 3.3 Task.WhenEach 需要 .NET 9+ ---");
#endif
    }

    static async Task WhenAllExample()
    {
        // 串行执行
        Console.WriteLine("❌ 串行执行:");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var user1 = await GetUserAsync(1);
        var order1 = await GetOrderAsync(1);

        sw.Stop();
        Console.WriteLine($"   结果: {user1}, {order1}");
        Console.WriteLine($"   耗时: {sw.ElapsedMilliseconds}ms\n");

        // 并发执行
        Console.WriteLine("✅ 并发执行:");
        sw.Restart();

        Task<string> userTask = GetUserAsync(2);
        Task<string> orderTask = GetOrderAsync(2);

        string[] results = await Task.WhenAll(userTask, orderTask);

        sw.Stop();
        Console.WriteLine($"   结果: {results[0]}, {results[1]}");
        Console.WriteLine($"   耗时: {sw.ElapsedMilliseconds}ms");
    }

    static async Task WhenAnyExample()
    {
        // 场景 1: 超时控制
        Console.WriteLine("⏱️ 超时控制示例:");
        try
        {
            var result = await GetDataWithTimeoutAsync();
            Console.WriteLine($"✓ 获取到数据: {result}");
        }
        catch (TimeoutException)
        {
            Console.WriteLine("⚠️ 请求超时");
        }

        // 场景 2: 多数据源竞速
        Console.WriteLine("\n🏎️ 多数据源竞速:");
        var fastestData = await GetFastestDataAsync();
        Console.WriteLine($"✓ 最快的数据源返回: {fastestData}");
    }

#if NET9_0_OR_GREATER
    static async Task WhenEachExample()
    {
        Task<int>[] tasks = new[]
        {
            Task.Run(async () => { await Task.Delay(2000); return 1; }),
            Task.Run(async () => { await Task.Delay(500); return 2; }),
            Task.Run(async () => { await Task.Delay(1000); return 3; })
        };

        Console.WriteLine("⏳ 按完成顺序处理任务:");
        await foreach (var completedTask in Task.WhenEach(tasks))
        {
            int result = await completedTask;
            Console.WriteLine($"✓ 任务完成，结果: {result}");
        }
    }
#endif

    // 辅助方法
    static async Task<string> GetUserAsync(int id)
    {
        await Task.Delay(1000);
        return $"User{id}";
    }

    static async Task<string> GetOrderAsync(int id)
    {
        await Task.Delay(1500);
        return $"Order{id}";
    }

    static async Task<string> GetDataWithTimeoutAsync()
    {
        Task<string> dataTask = GetDataFromSlowServiceAsync();
        Task delayTask = Task.Delay(2000);

        Task completedTask = await Task.WhenAny(dataTask, delayTask);

        if (completedTask == dataTask)
        {
            return await dataTask;
        }
        else
        {
            throw new TimeoutException("请求超时");
        }
    }

    static async Task<string> GetDataFromSlowServiceAsync()
    {
        await Task.Delay(3000);
        return "慢速服务的数据";
    }

    static async Task<string> GetFastestDataAsync()
    {
        var source1 = GetDataFromSource1Async();
        var source2 = GetDataFromSource2Async();
        var source3 = GetDataFromSource3Async();

        Task<string> winner = await Task.WhenAny(source1, source2, source3);
        return await winner;
    }

    static async Task<string> GetDataFromSource1Async()
    {
        await Task.Delay(1500);
        return "数据源 1";
    }

    static async Task<string> GetDataFromSource2Async()
    {
        await Task.Delay(800);
        return "数据源 2";
    }

    static async Task<string> GetDataFromSource3Async()
    {
        await Task.Delay(1200);
        return "数据源 3";
    }
}
