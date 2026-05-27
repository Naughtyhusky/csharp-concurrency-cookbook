namespace Locks.AdvancedLocks;

/// <summary>
/// SemaphoreSlim：信号量
/// 核心思想：控制"同时能有几个线程进入"，而不是"只有一个"
/// 支持异步 WaitAsync，是异步代码中最重要的同步原语之一
/// </summary>
public static class SemaphoreSlimDemo
{
    // ─── 1. 基本用法：限制并发度 ─────────────────────────────────────────────

    /// <summary>
    /// 场景：下载器，最多同时 3 个并发下载，超过的排队等待
    /// </summary>
    public static async Task ConcurrencyLimiterDemo()
    {
        Console.WriteLine("\n── SemaphoreSlim 并发限制（最多3个同时进行）──");

        // 初始计数=3，最大计数=3：同时最多3个线程通过
        using var semaphore = new SemaphoreSlim(initialCount: 3, maxCount: 3);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            await semaphore.WaitAsync(); // 异步等待：获取一个"令牌"
            try
            {
                Console.WriteLine($"  [{sw.Elapsed:ss\\.ff}s] 任务 {i,2} 开始（当前剩余令牌: {semaphore.CurrentCount}）");
                await Task.Delay(300); // 模拟工作
                Console.WriteLine($"  [{sw.Elapsed:ss\\.ff}s] 任务 {i,2} 完成");
            }
            finally
            {
                semaphore.Release(); // 释放令牌，让等待队列中的下一个进入
            }
        });

        await Task.WhenAll(tasks);
        Console.WriteLine($"  全部完成，总耗时: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("  如果不限流：10个同时跑，300ms结束；限流3个：需要约4轮，~1200ms");
        Console.WriteLine("  这正是限流的代价与价值：保护下游资源不被打垮");
    }

    // ─── 2. 实战：HTTP 并发限制器 ────────────────────────────────────────────

    /// <summary>
    /// 生产级场景：批量调用外部 API，但限制并发数，避免打垮对方服务
    /// </summary>
    public static async Task HttpConcurrencyLimiterDemo()
    {
        Console.WriteLine("\n── 实战：HTTP 并发限制器 ──");

        const int maxConcurrency = 5; // 最多 5 个并发 HTTP 请求
        using var semaphore = new SemaphoreSlim(maxConcurrency, maxConcurrency);

        var orderIds = Enumerable.Range(1, 20).ToList();
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var tasks = orderIds.Select(async orderId =>
        {
            await semaphore.WaitAsync();
            try
            {
                // 模拟 HTTP 请求（50-150ms）
                await Task.Delay(Random.Shared.Next(50, 150));
                string result = $"Order_{orderId}: OK";
                results.Add(result);
            }
            finally
            {
                semaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        Console.WriteLine($"  处理了 {results.Count} 个订单");
        Console.WriteLine($"  耗时: {sw.ElapsedMilliseconds} ms（并发={maxConcurrency}）");
        Console.WriteLine("  对比：并发=1（串行）约 2000ms，并发=20（无限）约 150ms");
    }

    // ─── 3. SemaphoreSlim 作为二进制信号量（互斥锁的异步版本）──────────────

    private static readonly SemaphoreSlim _asyncMutex = new(initialCount: 1, maxCount: 1);
    private static int _sharedResource = 0;

    /// <summary>
    /// 初始计数=1，最大计数=1 → 退化为互斥锁
    /// 这就是异步代码中 lock 的替代方案！
    /// </summary>
    public static async Task AsyncMutexDemo()
    {
        Console.WriteLine("\n── SemaphoreSlim(1,1)：异步互斥锁 ──");

        _sharedResource = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        var tasks = Enumerable.Range(0, 10).Select(async i =>
        {
            await _asyncMutex.WaitAsync(); // 异步等锁
            try
            {
                int current = _sharedResource;
                await Task.Delay(10); // 模拟异步工作（普通 lock 里不能 await！）
                _sharedResource = current + 1;
            }
            finally
            {
                _asyncMutex.Release();
            }
        });

        await Task.WhenAll(tasks);
        sw.Stop();

        Console.WriteLine($"  期望值: 10，实际值: {_sharedResource} {(_sharedResource == 10 ? "✅" : "❌")}");
        Console.WriteLine($"  耗时: {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("  普通 lock 里不能 await，这是 SemaphoreSlim 作为异步锁的核心价值");
    }

    // ─── 4. 超时和取消支持 ────────────────────────────────────────────────────

    public static async Task TimeoutAndCancellationDemo()
    {
        Console.WriteLine("\n── WaitAsync 超时与取消 ──");

        using var semaphore = new SemaphoreSlim(0, 1); // 初始0，没有令牌
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(300));

        Console.WriteLine("  尝试获取信号量（最多等 500ms，或取消令牌触发）...");

        try
        {
            // 带超时：false = 超时未获取到
            bool acquired = await semaphore.WaitAsync(TimeSpan.FromMilliseconds(500), cts.Token);
            Console.WriteLine(acquired ? "  ✅ 获取成功" : "  ⏰ 超时未获取");
        }
        catch (OperationCanceledException)
        {
            Console.WriteLine("  🚫 被取消令牌取消（300ms 触发，早于 500ms 超时）");
        }
    }

    // ─── 5. 常见陷阱 ──────────────────────────────────────────────────────────

    public static void PitfallDemo()
    {
        Console.WriteLine("\n── ⚠️ 常见陷阱 ──");
        Console.WriteLine("  ❌ 忘记 Release()：信号量计数永久减少，后续线程永远等不到");
        Console.WriteLine("     修复：始终用 try/finally 包裹，在 finally 中 Release()");
        Console.WriteLine();
        Console.WriteLine("  ❌ Release 次数超过 maxCount：");
        Console.WriteLine("     semaphore.Release() 超出 maxCount 会抛 SemaphoreFullException");
        Console.WriteLine();
        Console.WriteLine("  ❌ 把 SemaphoreSlim 用在无需限流的简单场景：");
        Console.WriteLine("     单线程场景、或者不关心并发度 → 直接用 lock 更简单");
    }

    public static async Task Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  SemaphoreSlim 信号量演示");
        Console.WriteLine("═══════════════════════════════════════════");

        await ConcurrencyLimiterDemo();
        await HttpConcurrencyLimiterDemo();
        await AsyncMutexDemo();
        await TimeoutAndCancellationDemo();
        PitfallDemo();
    }
}
