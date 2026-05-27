namespace Locks.Pitfalls;

/// <summary>
/// 锁的常见陷阱：错误的 lock 对象、死锁、SpinLock 值类型复制、SemaphoreSlim 泄漏
/// </summary>
public static class LockPitfallsDemo
{
    private static readonly object _lockA = new();
    private static readonly object _lockB = new();

    public static void WrongLockObjectDemo()
    {
        Console.WriteLine("\n── ❌ 陷阱 1：错误的 lock 对象 ──");
        Console.WriteLine("  ❌ lock(this)：锁对象是公开的，外部也能 lock 它 → 意外死锁");
        Console.WriteLine("  ❌ lock(typeof(T))：Type 对象进程级共享，跨类意外竞争");
        Console.WriteLine("  ❌ lock(\"常量字符串\")：CLR 字符串拘留，不同地方相同字面量是同一对象");
        Console.WriteLine("  ✅ 正确：private readonly object _lock = new object();");
    }

    public static void DeadlockDemo()
    {
        Console.WriteLine("\n── ❌ 陷阱 2：加锁顺序不一致导致死锁 ──");
        Console.WriteLine("  线程A：先锁 A，再锁 B");
        Console.WriteLine("  线程B：先锁 B，再锁 A  → 互相等待，永久阻塞");
        Console.WriteLine("  （以下用 TryEnter 超时模拟，不真正死锁）");

        var taskA = Task.Run(() =>
        {
            bool gotA = Monitor.TryEnter(_lockA, 100);
            if (!gotA) return "A: 未获取 LockA";
            try
            {
                Thread.Sleep(50);
                bool gotB = Monitor.TryEnter(_lockB, 150);
                if (!gotB) return "A: 无法获取 LockB（死锁预防，超时退出）";
                try { return "A: 成功获取两把锁"; }
                finally { Monitor.Exit(_lockB); }
            }
            finally { Monitor.Exit(_lockA); }
        });

        var taskB = Task.Run(() =>
        {
            bool gotB = Monitor.TryEnter(_lockB, 100);
            if (!gotB) return "B: 未获取 LockB";
            try
            {
                Thread.Sleep(50);
                bool gotA = Monitor.TryEnter(_lockA, 150);
                if (!gotA) return "B: 无法获取 LockA（死锁预防，超时退出）";
                try { return "B: 成功获取两把锁"; }
                finally { Monitor.Exit(_lockA); }
            }
            finally { Monitor.Exit(_lockB); }
        });

        Task.WhenAll(taskA, taskB).Wait();
        Console.WriteLine($"  结果A: {taskA.Result}");
        Console.WriteLine($"  结果B: {taskB.Result}");
        Console.WriteLine("  ✅ 预防：统一加锁顺序 / TryEnter + 超时回退 / 重构避免多锁");
    }

    public static void SpinLockRefPitfallDemo()
    {
        Console.WriteLine("\n── ❌ 陷阱 3：SpinLock 是值类型，传递必须用 ref ──");
        Console.WriteLine("  ❌ SpinLock localCopy = _lock;  // 复制！两个独立对象！");
        Console.WriteLine("  ❌ void DoWork(SpinLock sl)     // 参数传递也是复制！");
        Console.WriteLine("  ✅ void DoWork(ref SpinLock sl) // 正确：ref 传递");
    }

    public static async Task SemaphoreLeakDemo()
    {
        Console.WriteLine("\n── ❌ 陷阱 4：SemaphoreSlim 异常路径未 Release ──");
        Console.WriteLine("  ❌ await semaphore.WaitAsync();");
        Console.WriteLine("     await DoWork(); // 抛异常后，下面的 Release 不执行！");
        Console.WriteLine("     semaphore.Release();");
        Console.WriteLine("  ✅ 正确：");
        Console.WriteLine("     await semaphore.WaitAsync();");
        Console.WriteLine("     try { await DoWork(); }");
        Console.WriteLine("     finally { semaphore.Release(); }");

        using var semaphore = new SemaphoreSlim(1, 1);
        await semaphore.WaitAsync();
        try
        {
            await Task.Delay(10);
            Console.WriteLine($"  ✅ 工作完成，finally 将释放，CurrentCount={semaphore.CurrentCount}");
        }
        finally
        {
            semaphore.Release();
            Console.WriteLine($"  ✅ 已释放，CurrentCount={semaphore.CurrentCount}");
        }
    }

    public static async Task Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  锁的常见陷阱");
        Console.WriteLine("═══════════════════════════════════════════");

        WrongLockObjectDemo();
        DeadlockDemo();
        SpinLockRefPitfallDemo();
        await SemaphoreLeakDemo();
    }
}
