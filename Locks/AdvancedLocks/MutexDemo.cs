namespace Locks.AdvancedLocks;

/// <summary>
/// Mutex：互斥体（内核对象）
/// 核心特性：跨进程同步！这是它与 lock 最本质的区别
/// 代价：内核态切换，比 lock 慢约 1000 倍
/// </summary>
public static class MutexDemo
{
    public static void BasicMutexDemo()
    {
        Console.WriteLine("\n── Mutex 基本用法 ──");

        using var mutex = new Mutex();

        var tasks = Enumerable.Range(0, 3).Select(i => Task.Run(() =>
        {
            Console.WriteLine($"  [线程{i}] 等待 Mutex...");
            mutex.WaitOne();
            try
            {
                Console.WriteLine($"  [线程{i}] ✅ 获取 Mutex，工作中...");
                Thread.Sleep(80);
                Console.WriteLine($"  [线程{i}] 释放 Mutex");
            }
            finally { mutex.ReleaseMutex(); }
        }));

        Task.WhenAll(tasks).Wait();
    }

    public static void SingleInstanceDemo()
    {
        Console.WriteLine("\n── Mutex 核心用途：单实例应用 ──");

        const string mutexName = "Global\\MyConcurrencyCookbookApp_Demo";

        bool createdNew;
        using var mutex = new Mutex(initiallyOwned: true, name: mutexName, createdNew: out createdNew);

        if (!createdNew)
            Console.WriteLine("  ❌ 程序已在运行，拒绝启动第二个实例！");
        else
        {
            Console.WriteLine("  ✅ 这是第一个实例，正常启动");
            Console.WriteLine("  （再启动一个实例时，命名 Mutex 会拦截它）");
        }

        Console.WriteLine();
        Console.WriteLine("  Mutex vs lock 选择：");
        Console.WriteLine("  ┌──────────────────────────────────────────┐");
        Console.WriteLine("  │ 需求               │ 推荐                 │");
        Console.WriteLine("  ├──────────────────────────────────────────┤");
        Console.WriteLine("  │ 同一进程内线程同步 │ lock（快）            │");
        Console.WriteLine("  │ 异步方法内部同步   │ SemaphoreSlim.WaitAsync│");
        Console.WriteLine("  │ 跨进程互斥         │ Mutex（唯一选择）     │");
        Console.WriteLine("  └──────────────────────────────────────────┘");
    }

    public static void MutexVsLockPerformanceDemo()
    {
        Console.WriteLine("\n── Mutex vs lock 性能差异（内核态 vs 用户态）──");

        const int iterations = 5_000;
        var lockObj = new object();
        long c1 = 0, c2 = 0;

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < iterations; i++)
            lock (lockObj) { c1++; }
        sw.Stop();
        long lockTicks = sw.ElapsedTicks;

        using var mutex = new Mutex();
        sw.Restart();
        for (int i = 0; i < iterations; i++)
        {
            mutex.WaitOne();
            try { c2++; }
            finally { mutex.ReleaseMutex(); }
        }
        sw.Stop();
        long mutexTicks = sw.ElapsedTicks;

        Console.WriteLine($"  {iterations:N0} 次加锁：");
        Console.WriteLine($"  lock:  {lockTicks,8:N0} ticks");
        Console.WriteLine($"  Mutex: {mutexTicks,8:N0} ticks");
        double ratio = lockTicks > 0 ? (double)mutexTicks / lockTicks : 0;
        Console.WriteLine($"  Mutex 比 lock 慢约 {ratio:F0} 倍（每次需要用户态→内核态切换）");
        Console.WriteLine("  结论：除非跨进程，否则永远不要用 Mutex 做线程同步！");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  Mutex 互斥体演示");
        Console.WriteLine("═══════════════════════════════════════════");

        BasicMutexDemo();
        SingleInstanceDemo();
        MutexVsLockPerformanceDemo();
    }
}
