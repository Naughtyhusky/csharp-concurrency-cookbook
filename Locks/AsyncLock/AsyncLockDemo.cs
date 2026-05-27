namespace Locks.AsyncLock;

/// <summary>
/// 为什么 lock 不能在 async 方法中使用？
/// AsyncLocal 和 AsyncLock 的区别是什么？
/// 以及如何用 SemaphoreSlim 实现支持 using 语法的 AsyncLock
/// </summary>
public static class AsyncLockDemo
{
    // ─── 1. 演示 lock 里不能 await ───────────────────────────────────────────

    public static void WhyLockCantHaveAwaitDemo()
    {
        Console.WriteLine("\n── 为什么 lock 里不能 await ──");
        Console.WriteLine("  C# 编译器直接拒绝在 lock 块内使用 await：");
        Console.WriteLine("  CS1996: Cannot await in the body of a lock statement");
        Console.WriteLine();
        Console.WriteLine("  根本原因：");
        Console.WriteLine("  1. lock 基于线程归属（ThreadID）实现，线程 A 获锁、线程 A 释放");
        Console.WriteLine("  2. await 之后可能在【不同线程】上续接，导致无法释放锁");
        Console.WriteLine("  3. 即使 await 后在同一线程续接，持锁期间让出 CPU 也会导致");
        Console.WriteLine("     其他任务无法执行（相当于单线程死锁）");
        Console.WriteLine();
        Console.WriteLine("  ❌ 下面的代码无法编译：");
        Console.WriteLine("     lock (_obj)");
        Console.WriteLine("     {");
        Console.WriteLine("         await SomeAsyncMethod(); // CS1996 编译错误！");
        Console.WriteLine("     }");
    }

    // ─── 2. AsyncLocal<T> vs AsyncLock：彻底厘清两个概念 ────────────────────

    // AsyncLocal<T>：数据随 async 调用链"流动"，每条链有独立副本，无互斥
    private static readonly AsyncLocal<string> _currentUser = new();

    // SemaphoreSlim(1,1)：异步互斥锁，同时只有一个任务能进入
    private static readonly SemaphoreSlim _transferLock = new(1, 1);
    private static int _sharedBalance = 1000;

    public static async Task AsyncLocalVsAsyncLockDemo()
    {
        Console.WriteLine("\n── AsyncLocal<T> vs AsyncLock：两个完全不同的东西 ──");
        Console.WriteLine();
        Console.WriteLine("  AsyncLocal<T>  → 解决【数据如何随调用链传播】，每条链独立副本，无互斥");
        Console.WriteLine("  AsyncLock      → 解决【多个 async 任务互斥保护共享资源】，有等待");
        Console.WriteLine();

        // 演示 AsyncLocal：三条并发调用，各自读取自己的上下文数据，不互斥
        Console.WriteLine("  ─ AsyncLocal 演示：并发请求，各自读取自己的上下文数据 ─");

        async Task HandleRequest(string userId)
        {
            _currentUser.Value = userId;
            await Task.Delay(10); // await 后依然能拿到正确的上下文值
            Console.WriteLine($"  [AsyncLocal] {userId} 读取到上下文用户: {_currentUser.Value}");
        }

        // 三条链并发，没有任何互斥，各自独立
        await Task.WhenAll(
            HandleRequest("user_A"),
            HandleRequest("user_B"),
            HandleRequest("user_C")
        );

        // 演示 AsyncLock（SemaphoreSlim）：三个任务互斥，必须排队
        Console.WriteLine();
        Console.WriteLine("  ─ AsyncLock 演示：并发转账，必须排队，保护共享余额 ─");
        _sharedBalance = 1000;

        async Task TransferAsync(string userId, int amount)
        {
            await _transferLock.WaitAsync(); // 排队等待，同时只有一个进入
            try
            {
                int before = _sharedBalance;
                await Task.Delay(10); // 模拟数据库操作
                _sharedBalance = before - amount;
                Console.WriteLine($"  [AsyncLock] {userId} 转出 {amount}，余额: {before} → {_sharedBalance}");
            }
            finally { _transferLock.Release(); }
        }

        await Task.WhenAll(
            TransferAsync("user_A", 100),
            TransferAsync("user_B", 200),
            TransferAsync("user_C", 300)
        );
        Console.WriteLine($"  最终余额: {_sharedBalance}（期望: 400）{(_sharedBalance == 400 ? "✅" : "❌")}");
        Console.WriteLine();
        Console.WriteLine("  总结：");
        Console.WriteLine("  AsyncLocal<T> → 数据隔离，不同调用链互不干扰，完全不互斥");
        Console.WriteLine("  AsyncLock     → 访问互斥，同时只有一个任务能进临界区");
        Console.WriteLine("  它们解决完全不同的问题，不存在谁替代谁");
    }

    // ─── 3. 完整的 AsyncLock 实现 ─────────────────────────────────────────────

    /// <summary>
    /// 基于 SemaphoreSlim 的异步锁，支持 using 语法
    /// 使用方式：using (await _asyncLock.LockAsync()) { ... }
    /// </summary>
    public sealed class AsyncLock
    {
        private readonly SemaphoreSlim _semaphore = new(1, 1);

        public async Task<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            await _semaphore.WaitAsync(cancellationToken);
            return new Releaser(_semaphore);
        }

        private sealed class Releaser : IDisposable
        {
            private readonly SemaphoreSlim _semaphore;
            private bool _disposed;

            internal Releaser(SemaphoreSlim semaphore) => _semaphore = semaphore;

            public void Dispose()
            {
                if (!_disposed)
                {
                    _semaphore.Release();
                    _disposed = true;
                }
            }
        }
    }

    // ─── 4. AsyncLock 的使用演示 ──────────────────────────────────────────────

    private static readonly AsyncLock _asyncLock = new();
    private static int _sharedValue = 0;

    public static async Task AsyncLockUsageDemo()
    {
        Console.WriteLine("\n── AsyncLock 使用演示 ──");

        _sharedValue = 0;

        var tasks = Enumerable.Range(1, 10).Select(async i =>
        {
            using (await _asyncLock.LockAsync())
            {
                int current = _sharedValue;
                await Task.Delay(10); // ✅ 锁内可以自由 await！
                _sharedValue = current + 1;
                Console.WriteLine($"  [任务{i,2}] 写入完成，当前值: {_sharedValue}");
            }
        });

        await Task.WhenAll(tasks);
        Console.WriteLine($"  最终值: {_sharedValue}（期望 10）{(_sharedValue == 10 ? "✅" : "❌")}");
    }

    // ─── 5. 对比：直接使用 SemaphoreSlim（不封装）────────────────────────────

    private static readonly SemaphoreSlim _rawSemaphore = new(1, 1);
    private static int _sharedValue2 = 0;

    public static async Task DirectSemaphoreSlimDemo()
    {
        Console.WriteLine("\n── 直接用 SemaphoreSlim 作为异步锁（无需封装）──");

        _sharedValue2 = 0;

        var tasks = Enumerable.Range(1, 5).Select(async i =>
        {
            await _rawSemaphore.WaitAsync();
            try
            {
                int current = _sharedValue2;
                await Task.Delay(20);
                _sharedValue2 = current + 1;
                Console.WriteLine($"  [任务{i}] 完成，当前值: {_sharedValue2}");
            }
            finally
            {
                _rawSemaphore.Release();
            }
        });

        await Task.WhenAll(tasks);
        Console.WriteLine($"  最终值: {_sharedValue2}（期望 5）{(_sharedValue2 == 5 ? "✅" : "❌")}");
        Console.WriteLine("  两种写法等价，AsyncLock 封装让代码更像 lock，不容易忘记 Release");
    }

    // ─── 6. 带超时和取消的 AsyncLock ─────────────────────────────────────────

    public static async Task AsyncLockWithTimeoutDemo()
    {
        Console.WriteLine("\n── AsyncLock 带超时与取消 ──");

        using var semaphore = new SemaphoreSlim(1, 1);
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(200));

        await semaphore.WaitAsync();

        Console.WriteLine("  锁已被占用，另一个任务尝试获取（超时 500ms，CTS 200ms）...");

        var waiter = Task.Run(async () =>
        {
            try
            {
                bool acquired = await semaphore.WaitAsync(500, cts.Token);
                Console.WriteLine(acquired ? "  ✅ 获取成功" : "  ⏰ 超时");
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("  🚫 取消令牌触发（200ms），早于超时（500ms）");
            }
        });

        await waiter;
        semaphore.Release();
    }

    public static async Task Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  异步锁（AsyncLock）演示");
        Console.WriteLine("═══════════════════════════════════════════");

        WhyLockCantHaveAwaitDemo();
        await AsyncLocalVsAsyncLockDemo();
        await AsyncLockUsageDemo();
        await DirectSemaphoreSlimDemo();
        await AsyncLockWithTimeoutDemo();
    }
}
