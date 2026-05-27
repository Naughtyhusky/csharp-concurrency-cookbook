using System.Collections.Concurrent;

namespace Locks.LockBasics;

/// <summary>
/// lock 关键字 与 Monitor 完全解析
/// lock 只是 Monitor.Enter + Monitor.Exit 的语法糖
/// </summary>
public static class LockAndMonitorDemo
{
    // ─── 1. lock 的本质：Monitor 的语法糖 ──────────────────────────────────

    private static readonly object _lock = new();
    private static int _counter = 0;

    /// <summary>
    /// lock 编译后等价于 Monitor.Enter + try/finally + Monitor.Exit
    /// 下面两种写法完全等价
    /// </summary>
    public static void LockEssenceDemo()
    {
        Console.WriteLine("\n── lock 的本质 ──");

        // 写法 1：lock 语法糖（推荐）
        lock (_lock)
        {
            _counter++;
            Console.WriteLine($"  [lock] counter = {_counter}");
        }

        // 写法 2：等价的 Monitor 展开形式（lock 编译后就是这样）
        bool lockTaken = false;
        try
        {
            Monitor.Enter(_lock, ref lockTaken);
            _counter++;
            Console.WriteLine($"  [Monitor] counter = {_counter}");
        }
        finally
        {
            if (lockTaken) Monitor.Exit(_lock);
        }

        Console.WriteLine("  结论：永远用 lock，不要手写 Monitor.Enter/Exit，除非你需要 TryEnter");
    }

    // ─── 2. 多线程竞争演示：有锁 vs 无锁 ──────────────────────────────────

    private static long _unsafeCounter = 0;
    private static long _safeCounter = 0;
    private static readonly object _safeLock = new();

    /// <summary>
    /// 用 10 个线程同时累加 100 次，演示有锁和无锁的结果差异
    /// </summary>
    public static void ThreadSafetyDemo()
    {
        Console.WriteLine("\n── 有锁 vs 无锁的计数器 ──");

        const int threads = 10;
        const int iterations = 10_000;
        _unsafeCounter = 0;
        _safeCounter = 0;

        // 无锁版本
        var unsafeTasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
                _unsafeCounter++; // 非线程安全：读-改-写三步，非原子
        }));
        Task.WhenAll(unsafeTasks).Wait();

        // 有锁版本
        var safeTasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
                lock (_safeLock) { _safeCounter++; }
        }));
        Task.WhenAll(safeTasks).Wait();

        long expected = threads * iterations;
        Console.WriteLine($"  期望值:   {expected:N0}");
        Console.WriteLine($"  无锁结果: {_unsafeCounter:N0} {(_unsafeCounter == expected ? "✅" : "❌（数据丢失！）")}");
        Console.WriteLine($"  有锁结果: {_safeCounter:N0} {(_safeCounter == expected ? "✅" : "❌")}");
    }

    // ─── 3. Monitor.TryEnter：带超时的尝试获锁 ─────────────────────────────

    private static readonly object _resourceLock = new();

    /// <summary>
    /// TryEnter 允许你"试着获取锁，获不到就算了"，避免永久阻塞
    /// 在需要避免死锁的场景非常有用
    /// </summary>
    public static void TryEnterDemo()
    {
        Console.WriteLine("\n── Monitor.TryEnter 避免永久阻塞 ──");

        // 线程 A 持有锁
        Task.Run(() =>
        {
            lock (_resourceLock)
            {
                Console.WriteLine("  [线程A] 获得锁，持续工作 500ms...");
                Thread.Sleep(500);
                Console.WriteLine("  [线程A] 释放锁");
            }
        });

        Thread.Sleep(50); // 确保线程A先拿到锁

        // 线程 B 尝试获锁，最多等 200ms
        Task.Run(() =>
        {
            bool acquired = Monitor.TryEnter(_resourceLock, TimeSpan.FromMilliseconds(200));
            if (acquired)
            {
                try
                {
                    Console.WriteLine("  [线程B] ✅ 成功获锁");
                }
                finally
                {
                    Monitor.Exit(_resourceLock);
                }
            }
            else
            {
                Console.WriteLine("  [线程B] ⏰ 超时未获锁，跳过处理（避免阻塞）");
            }
        }).Wait();

        Thread.Sleep(600); // 等线程A结束
    }

    // ─── 4. Monitor.Wait / Pulse：生产者-消费者协调 ─────────────────────────

    private static readonly Queue<int> _queue = new();
    private static readonly object _queueLock = new();
    private static bool _producerDone = false;

    /// <summary>
    /// Monitor.Wait 释放锁并等待通知；Monitor.Pulse 通知一个等待线程
    /// 这是最原始的"条件变量"机制，现代代码推荐用 Channel 替代
    /// </summary>
    public static void WaitPulseDemo()
    {
        Console.WriteLine("\n── Monitor.Wait/Pulse 生产者-消费者 ──");

        _producerDone = false;
        _queue.Clear();

        // 生产者
        var producer = Task.Run(() =>
        {
            for (int i = 1; i <= 5; i++)
            {
                lock (_queueLock)
                {
                    _queue.Enqueue(i);
                    Console.WriteLine($"  [生产者] 放入 {i}");
                    Monitor.Pulse(_queueLock); // 通知消费者"有数据了"
                }
                Thread.Sleep(100);
            }
            lock (_queueLock)
            {
                _producerDone = true;
                Monitor.PulseAll(_queueLock); // 通知消费者"结束了"
            }
        });

        // 消费者
        var consumer = Task.Run(() =>
        {
            while (true)
            {
                int item;
                lock (_queueLock)
                {
                    // 没有数据就等待（Wait 会释放锁，被 Pulse 唤醒后重新获锁）
                    while (_queue.Count == 0 && !_producerDone)
                        Monitor.Wait(_queueLock);

                    if (_queue.Count == 0 && _producerDone) break;

                    item = _queue.Dequeue();
                }
                Console.WriteLine($"  [消费者] 取出 {item}");
            }
            Console.WriteLine("  [消费者] 结束");
        });

        Task.WhenAll(producer, consumer).Wait();
        Console.WriteLine("  💡 现代代码推荐用 Channel<T>，比 Monitor.Wait/Pulse 更安全清晰（见第09章）");
    }

    // ─── 5. Monitor 可重入性演示 ────────────────────────────────────────────

    private static readonly object _reentrantLock = new();

    static void OuterMethod()
    {
        lock (_reentrantLock)
        {
            Console.WriteLine("  [外层] 获取锁，调用内层方法...");
            InnerMethod(); // 同一线程再次请求同一把锁
            Console.WriteLine("  [外层] 内层返回，准备释放锁");
        }
        // 外层 lock 退出：递归计数归零，锁正式释放
    }

    static void InnerMethod()
    {
        lock (_reentrantLock) // ✅ 同一线程，递归计数 +1，不会死锁
        {
            Console.WriteLine("  [内层] ✅ 同线程重入成功，递归计数=2");
        }
        // 内层 lock 退出：递归计数 -1 = 1，锁还在外层持有
    }

    public static void ReentrantLockDemo()
    {
        Console.WriteLine("\n── Monitor 可重入演示（同线程嵌套 lock 不死锁）──");
        OuterMethod();
        Console.WriteLine("  递归计数归零，锁已完全释放");
    }

    // ─── 6. .NET 9 专属 Lock 类演示 ─────────────────────────────────────────

    private static readonly Lock _newLock = new Lock();
    private static int _newCounter = 0;

    public static void DotNet9LockDemo()
    {
        Console.WriteLine("\n── .NET 9 专属 Lock 类演示 ──");

        // 写法一：和 lock(object) 完全一样的语法，编译器自动优化
        lock (_newLock)
        {
            _newCounter++;
            Console.WriteLine($"  lock 语法：_newCounter = {_newCounter}");
        }

        // 写法二：EnterScope + using（更现代，更明确，推荐）
        using (_newLock.EnterScope())
        {
            _newCounter++;
            Console.WriteLine($"  EnterScope 语法：_newCounter = {_newCounter}");
        }

        // TryEnter 带超时
        if (_newLock.TryEnter(TimeSpan.FromMilliseconds(100)))
        {
            try
            {
                Console.WriteLine($"  TryEnter 成功，IsHeldByCurrentThread = {_newLock.IsHeldByCurrentThread}");
            }
            finally { _newLock.Exit(); }
        }

        Console.WriteLine($"  锁释放后，IsHeldByCurrentThread = {_newLock.IsHeldByCurrentThread}");
        Console.WriteLine("  💡 .NET 9+ 新代码推荐使用 Lock 类，语义专一，API 丰富");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  lock 与 Monitor 完全解析");
        Console.WriteLine("═══════════════════════════════════════════");

        LockEssenceDemo();
        ThreadSafetyDemo();
        ReentrantLockDemo();
        TryEnterDemo();
        WaitPulseDemo();
        DotNet9LockDemo();
    }
}
