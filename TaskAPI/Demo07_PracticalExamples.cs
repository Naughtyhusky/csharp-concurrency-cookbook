namespace TaskAPI;

/// <summary>
/// 示例 7: 实战演练 - 综合示例
/// </summary>
public static class Demo07_PracticalExamples
{
    public static async Task RunAsync()
    {
        Console.WriteLine("\n--- 7.1 并发下载多个文件 ---");
        await Example1_ParallelDownloads();

        Console.WriteLine("\n--- 7.2 带重试的任务执行 ---");
        await Example2_RetryPattern();

        Console.WriteLine("\n--- 7.3 批量处理的限流控制 ---");
        await Example3_ThrottledProcessing();
    }

    // 示例 1: 并发下载多个文件
    static async Task Example1_ParallelDownloads()
    {
        string[] urls = new[]
        {
            "https://example.com/file1",
            "https://example.com/file2",
            "https://example.com/file3",
            "https://example.com/file4"
        };

        Console.WriteLine($"⏳ 并发下载 {urls.Length} 个文件...");
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // 创建所有下载任务（模拟）
        Task<string>[] tasks = urls.Select(url =>
            SimulateDownloadAsync(url)
        ).ToArray();

        // 并发执行
        string[] results = await Task.WhenAll(tasks);
        sw.Stop();

        for (int i = 0; i < results.Length; i++)
        {
            Console.WriteLine($"  ✓ 文件 {i + 1}: {results[i]}");
        }
        Console.WriteLine($"✓ 总耗时: {sw.ElapsedMilliseconds}ms");
    }

    static async Task<string> SimulateDownloadAsync(string url)
    {
        // 模拟下载延迟
        await Task.Delay(Random.Shared.Next(500, 1500));
        int size = Random.Shared.Next(100, 1000);
        return $"{url} ({size} KB)";
    }

    // 示例 2: 带重试的任务执行
    static async Task Example2_RetryPattern()
    {
        Console.WriteLine("⏳ 执行可能失败的操作（最多重试 3 次）...");

        try
        {
            var result = await ExecuteWithRetryAsync(async () =>
            {
                return await UnreliableOperationAsync();
            }, maxRetries: 3);

            Console.WriteLine($"✓ 最终成功: {result}");
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"❌ 达到最大重试次数: {ex.Message}");
        }
    }

    static async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> operation,
        int maxRetries = 3)
    {
        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                return await operation();
            }
            catch (Exception ex) when (i < maxRetries - 1)
            {
                int delayMs = 500 * (i + 1);
                Console.WriteLine($"  ⚠️ 第 {i + 1} 次失败: {ex.Message}, {delayMs}ms 后重试...");
                await Task.Delay(delayMs);
            }
        }

        throw new InvalidOperationException("达到最大重试次数");
    }

    static int _attemptCount = 0;
    static async Task<string> UnreliableOperationAsync()
    {
        _attemptCount++;
        await Task.Delay(200);

        // 前两次失败，第三次成功
        if (_attemptCount < 3)
        {
            throw new Exception($"模拟失败 (尝试 {_attemptCount})");
        }

        return $"成功 (尝试 {_attemptCount})";
    }

    // 示例 3: 批量处理的限流控制
    static async Task Example3_ThrottledProcessing()
    {
        int[] items = Enumerable.Range(1, 20).ToArray();
        int maxConcurrency = 5;

        Console.WriteLine($"⏳ 处理 {items.Length} 个项目，最大并发度: {maxConcurrency}");

        var sw = System.Diagnostics.Stopwatch.StartNew();

        await ProcessItemsWithThrottleAsync(
            items,
            async item =>
            {
                await Task.Delay(500);
                Console.WriteLine($"  ✓ 处理项目 {item} (线程 {Thread.CurrentThread.ManagedThreadId})");
            },
            maxConcurrency
        );

        sw.Stop();
        Console.WriteLine($"✓ 总耗时: {sw.ElapsedMilliseconds}ms");
        Console.WriteLine($"💡 如果串行执行需要: {items.Length * 500}ms");
        Console.WriteLine($"💡 实际耗时约: {(items.Length / maxConcurrency) * 500}ms");
    }

    static async Task ProcessItemsWithThrottleAsync<T>(
        IEnumerable<T> items,
        Func<T, Task> processor,
        int maxConcurrency = 5)
    {
        using SemaphoreSlim semaphore = new SemaphoreSlim(maxConcurrency);

        var tasks = items.Select(async item =>
        {
            await semaphore.WaitAsync();
            try
            {
                await processor(item);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
    }
}
