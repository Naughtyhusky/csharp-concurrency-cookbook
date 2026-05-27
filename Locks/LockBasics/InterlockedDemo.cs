namespace Locks.LockBasics;

/// <summary>
/// Interlocked：无锁原子操作
/// 核心优势：CPU 指令级原子性，没有锁开销，是所有同步手段里最快的
/// 核心限制：只能操作单个变量；多变量复合操作必须用锁
/// </summary>
public static class InterlockedDemo
{
    // ─── 1. 计数器：最常见用法 ──────────────────────────────────────────────

    private static long _requestCount = 0;
    private static long _errorCount = 0;
    private static long _totalBytes = 0;

    public static void CounterDemo()
    {
        Console.WriteLine("\n── Interlocked 计数器演示 ──");
        Console.WriteLine("  10 个线程各自累加 100,000 次");

        _requestCount = 0;
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100_000; i++)
                Interlocked.Increment(ref _requestCount); // 原子 ++
        }));

        Task.WhenAll(tasks).Wait();
        Console.WriteLine($"  期望: 1,000,000  实际: {_requestCount:N0}  ✅ {(_requestCount == 1_000_000 ? "完全正确" : "❌ 有丢失")}");

        // 对比：普通 ++ 的结果
        long unsafeCount = 0;
        var unsafeTasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 100_000; i++)
                unsafeCount++; // 非原子，数据竞争
        }));
        Task.WhenAll(unsafeTasks).Wait();
        Console.WriteLine($"  非原子++：期望 1,000,000  实际: {unsafeCount:N0}  ← 每次运行都不同");
    }

    // ─── 2. Exchange：原子赋值 ───────────────────────────────────────────────

    public static void ExchangeDemo()
    {
        Console.WriteLine("\n── Interlocked.Exchange 原子赋值 ──");

        long value = 0;

        var tasks = Enumerable.Range(1, 5).Select(i => Task.Run(() =>
        {
            long old = Interlocked.Exchange(ref value, i); // 原子赋值，返回旧值
            Console.WriteLine($"  [线程{i}] 把 value 从 {old} 改为 {i}");
        }));

        Task.WhenAll(tasks).Wait();
        Console.WriteLine($"  最终 value = {value}（由最后一个写入的线程决定）");
    }

    // ─── 3. CompareExchange（CAS）：条件原子赋值 ──────────────────────────────

    public static void CompareExchangeDemo()
    {
        Console.WriteLine("\n── Interlocked.CompareExchange (CAS) ──");

        // 场景：多个线程争抢"启动权"，只有一个能成功
        int isStarted = 0; // 0 = 未启动，1 = 已启动
        int successCount = 0;

        var tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
        {
            // 如果当前是 0，就改为 1；返回旧值
            int prev = Interlocked.CompareExchange(ref isStarted, value: 1, comparand: 0);
            if (prev == 0)
            {
                // prev == 0：说明我是第一个，成功"抢到"启动权
                Interlocked.Increment(ref successCount);
                Console.WriteLine($"  [线程{i}] ✅ 获得启动权！");
            }
            else
            {
                Console.WriteLine($"  [线程{i}] ❌ 已有线程启动，跳过");
            }
        }));

        Task.WhenAll(tasks).Wait();
        Console.WriteLine($"  成功启动次数: {successCount}（应该精确等于 1）");
    }

    // ─── 4. 无锁懒加载（LazyInit 模式）────────────────────────────────────────

    private static string? _cachedConfig = null;

    public static string GetOrLoadConfig()
    {
        // 快路径：已加载直接返回（无锁）
        if (_cachedConfig != null) return _cachedConfig;

        // 慢路径：加载配置
        string loaded = $"Config_LoadedAt_{DateTime.Now:HH:mm:ss.fff}";
        Thread.Sleep(10); // 模拟加载耗时

        // CAS：只有还没人写入（null）时才写入我的值
        // 如果别的线程先写了，就用它的值，我的值丢弃
        Interlocked.CompareExchange(ref _cachedConfig, loaded, comparand: null);

        return _cachedConfig!;
    }

    public static void LazyInitDemo()
    {
        Console.WriteLine("\n── 无锁懒加载（CAS 模式）──");

        _cachedConfig = null;
        var results = new System.Collections.Concurrent.ConcurrentBag<string>();

        // 10 个线程并发初始化
        var tasks = Enumerable.Range(0, 10).Select(_ => Task.Run(() =>
        {
            results.Add(GetOrLoadConfig());
        }));

        Task.WhenAll(tasks).Wait();

        var distinct = results.Distinct().ToList();
        Console.WriteLine($"  10 个线程全部返回: {distinct.Count} 种不同值");
        Console.WriteLine($"  最终缓存值: {_cachedConfig}");
        Console.WriteLine("  ✅ 虽然可能有多个线程都执行了加载，但最终结果一致（线程安全）");
    }

    // ─── 5. Interlocked vs lock 性能对比 ─────────────────────────────────────

    public static void PerformanceComparisonDemo()
    {
        Console.WriteLine("\n── Interlocked vs lock 性能对比 ──");

        const int iterations = 5_000_000;
        long counter1 = 0, counter2 = 0;
        var lockObj = new object();

        // Interlocked
        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            Interlocked.Increment(ref counter1);
        sw.Stop();
        long interlockedMs = sw.ElapsedMilliseconds;
        long interlockedTicks = sw.ElapsedTicks;

        // lock
        sw.Restart();
        for (int i = 0; i < iterations; i++)
            lock (lockObj) { counter2++; }
        sw.Stop();
        long lockMs = sw.ElapsedMilliseconds;
        long lockTicks = sw.ElapsedTicks;

        Console.WriteLine($"  {iterations:N0} 次操作：");
        Console.WriteLine($"  Interlocked.Increment : {interlockedMs,4} ms ({interlockedTicks,10:N0} ticks)");
        Console.WriteLine($"  lock + counter++      : {lockMs,4} ms ({lockTicks,10:N0} ticks)");
        double ratio = lockTicks > 0 && interlockedTicks > 0 ? (double)lockTicks / interlockedTicks : 0;
        Console.WriteLine($"  lock 比 Interlocked 慢约 {ratio:F1} 倍（在单变量操作上）");
        Console.WriteLine("  结论：单变量操作，永远用 Interlocked！");
    }

    // ─── 6. Interlocked 的边界：多变量时必须用 lock ────────────────────────────

    public static void LimitationDemo()
    {
        Console.WriteLine("\n── ⚠️ Interlocked 的局限：多变量时失效 ──");

        long count = 0, total = 0;
        var lockObj = new object();

        // ❌ 错误：两个 Interlocked 操作之间存在窗口
        // 另一个线程可能在两次 Interlocked 之间插入，导致 count 和 total 不一致
        Console.WriteLine("  ❌ 错误写法（两个 Interlocked 不是整体原子的）：");
        Console.WriteLine("     Interlocked.Increment(ref count);");
        Console.WriteLine("     Interlocked.Add(ref total, amount);");
        Console.WriteLine("     → 这两步之间可能被其他线程打断！");

        // ✅ 正确：lock 保护复合操作
        Console.WriteLine("  ✅ 正确写法（lock 保证两个变量的整体一致性）：");
        Console.WriteLine("     lock (_lock) { count++; total += amount; }");

        // 验证：并发更新 count 和 total，保持 total == count * 10 的不变式
        count = 0; total = 0;
        var tasks = Enumerable.Range(0, 5).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < 10_000; i++)
                lock (lockObj) { count++; total += 10; }
        }));

        Task.WhenAll(tasks).Wait();
        bool invariant = total == count * 10;
        Console.WriteLine($"  count={count:N0}, total={total:N0}, total==count*10: {(invariant ? "✅" : "❌")}");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  Interlocked 无锁原子操作演示");
        Console.WriteLine("═══════════════════════════════════════════");

        CounterDemo();
        ExchangeDemo();
        CompareExchangeDemo();
        LazyInitDemo();
        PerformanceComparisonDemo();
        LimitationDemo();
    }
}
