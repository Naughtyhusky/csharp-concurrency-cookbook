using System.Diagnostics;

namespace AsyncAwait;

/// <summary>
/// Demo07: 实战示例
/// </summary>
public static class Demo07_PracticalExamples
{
    public static async Task Run()
    {
        Console.WriteLine("\n=== Demo07: 实战示例 ===\n");

        // 7.1 带缓存的异步方法
        Console.WriteLine("--- 7.1 带缓存的异步方法（ValueTask）---");
        await CachedServiceExample();

        // 7.2 并发控制（SemaphoreSlim）
        Console.WriteLine("\n--- 7.2 并发控制（SemaphoreSlim）---");
        await ConcurrencyControlExample();

        // 7.3 超时控制
        Console.WriteLine("\n--- 7.3 超时控制 ---");
        await TimeoutExample();

        // 7.4 重试逻辑
        Console.WriteLine("\n--- 7.4 重试逻辑 ---");
        await RetryExample();
    }

    // 示例 1：带缓存的异步方法
    static async Task CachedServiceExample()
    {
        var service = new CachedDataService();

        Console.WriteLine("✓ 首次调用（缓存未命中）:");
        var sw1 = Stopwatch.StartNew();
        string data1 = await service.GetDataAsync("key1");
        sw1.Stop();
        Console.WriteLine($"  结果: {data1}, 耗时: {sw1.ElapsedMilliseconds} ms");

        Console.WriteLine("\n✓ 第二次调用（缓存命中）:");
        var sw2 = Stopwatch.StartNew();
        string data2 = await service.GetDataAsync("key1");
        sw2.Stop();
        Console.WriteLine($"  结果: {data2}, 耗时: {sw2.ElapsedMilliseconds} ms（更快，无 I/O）");
    }

    // 示例 2：并发控制
    static async Task ConcurrencyControlExample()
    {
        var client = new ThrottledHttpClient(maxConcurrency: 3);
        var urls = Enumerable.Range(1, 10).Select(i => $"https://example.com/api/{i}").ToList();

        Console.WriteLine($"✓ 下载 {urls.Count} 个 URL（最多 3 个并发）");

        var sw = Stopwatch.StartNew();
        var results = await client.DownloadAllAsync(urls);
        sw.Stop();

        Console.WriteLine($"  ✅ 完成，总耗时: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine($"  💡 SemaphoreSlim 限制了并发数，避免资源耗尽");
    }

    // 示例 3：超时控制
    static async Task TimeoutExample()
    {
        var service = new TimeoutService();

        // 测试 1：正常完成
        Console.WriteLine("✓ 测试 1: 正常完成");
        try
        {
            string result1 = await TimeoutService.GetDataWithTimeoutAsync("fast", TimeSpan.FromSeconds(2));
            Console.WriteLine($"  ✅ 结果: {result1}");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"  ❌ 超时: {ex.Message}");
        }

        // 测试 2：超时
        Console.WriteLine("\n✓ 测试 2: 超时");
        try
        {
            string result2 = await TimeoutService.GetDataWithTimeoutAsync("slow", TimeSpan.FromSeconds(1));
            Console.WriteLine($"  ✅ 结果: {result2}");
        }
        catch (TimeoutException ex)
        {
            Console.WriteLine($"  ❌ 超时: {ex.Message}");
        }
    }

    // 示例 4：重试逻辑
    static async Task RetryExample()
    {
        var service = new RetryService();

        Console.WriteLine("✓ 重试失败的操作（最多 3 次）:");
        try
        {
            await service.ExecuteWithRetryAsync();
            Console.WriteLine("  ✅ 操作成功");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ❌ 操作失败: {ex.Message}");
        }
    }
}

// ======================== 实战类 ========================

/// <summary>
/// 带缓存的数据服务（使用 ValueTask）
/// </summary>
public class CachedDataService
{
    private readonly Dictionary<string, string> _cache = new();

    public async ValueTask<string> GetDataAsync(string key)
    {
        // 缓存命中（同步返回，无分配）
        if (_cache.TryGetValue(key, out string? cached))
        {
            return cached;
        }

        // 缓存未命中（模拟异步获取）
        await Task.Delay(500);
        string value = $"Data-{key}";
        _cache[key] = value;
        return value;
    }
}

/// <summary>
/// 限流 HTTP 客户端（使用 SemaphoreSlim）
/// </summary>
public class ThrottledHttpClient
{
    private readonly SemaphoreSlim _semaphore;

    public ThrottledHttpClient(int maxConcurrency)
    {
        _semaphore = new SemaphoreSlim(maxConcurrency);
    }

    public async Task<string> GetAsync(string url)
    {
        await _semaphore.WaitAsync();
        try
        {
            // 模拟 HTTP 请求
            await Task.Delay(200);
            return $"Content-{url}";
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<List<string>> DownloadAllAsync(List<string> urls)
    {
        var tasks = urls.Select(url => GetAsync(url));
        return (await Task.WhenAll(tasks)).ToList();
    }
}

/// <summary>
/// 超时控制服务
/// </summary>
public class TimeoutService
{
    public static async Task<string> GetDataWithTimeoutAsync(string key, TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);

        try
        {
            // 模拟异步操作
            int delay = key == "slow" ? 3000 : 500;
            await Task.Delay(delay, cts.Token);
            return $"Data-{key}";
        }
        catch (OperationCanceledException) when (cts.Token.IsCancellationRequested)
        {
            throw new TimeoutException($"操作超时: {key}");
        }
    }

    // 方案 2：使用 Task.WhenAny
    public async Task<string> GetDataWithTimeoutAlternativeAsync(string key, TimeSpan timeout)
    {
        var dataTask = GetDataSlowAsync(key);
        var timeoutTask = Task.Delay(timeout);

        var completedTask = await Task.WhenAny(dataTask, timeoutTask);

        if (completedTask == timeoutTask)
        {
            throw new TimeoutException($"操作超时: {key}");
        }

        return await dataTask;
    }

    private async Task<string> GetDataSlowAsync(string key)
    {
        await Task.Delay(3000);
        return $"Data-{key}";
    }
}

/// <summary>
/// 重试服务
/// </summary>
public class RetryService
{
    private int _attemptCount = 0;

    public async Task ExecuteWithRetryAsync(int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                Console.WriteLine($"  尝试 #{attempt}...");
                await UnreliableOperationAsync();
                Console.WriteLine($"  ✅ 成功");
                return; // 成功，退出
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ❌ 失败: {ex.Message}");

                if (attempt >= maxRetries)
                {
                    Console.WriteLine($"  ⚠️ 已达到最大重试次数");
                    throw; // 重新抛出异常
                }

                // 指数退避
                int delayMs = (int)Math.Pow(2, attempt) * 100;
                Console.WriteLine($"  ⏳ 等待 {delayMs} ms 后重试...");
                await Task.Delay(delayMs);
            }
        }
    }

    private async Task UnreliableOperationAsync()
    {
        await Task.Delay(100);

        _attemptCount++;

        // 前 2 次失败，第 3 次成功
        if (_attemptCount < 3)
        {
            throw new Exception("模拟临时失败");
        }
    }
}
