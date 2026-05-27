namespace Locks.LockBasics;

/// <summary>
/// SpinLock：自旋锁演示
/// 适用场景：极短临界区（< 100ns），避免线程切换开销
/// 注意：SpinLock 是值类型，必须用 ref 传递！
/// </summary>
public static class SpinLockDemo
{
    // ─── 1. SpinLock 基本用法 ────────────────────────────────────────────────

    // ⚠️ 注意：SpinLock 是 struct（值类型），绝对不能复制！
    // 必须声明为字段，且传递时必须用 ref
    private static SpinLock _spinLock = new SpinLock(enableThreadOwnerTracking: false);
    private static long _spinCounter = 0;

    /// <summary>
    /// SpinLock 的正确使用模式
    /// </summary>
    public static void BasicSpinLockDemo()
    {
        Console.WriteLine("\n── SpinLock 基本用法 ──");

        const int threads = 8;
        const int iterations = 100_000;
        _spinCounter = 0;

        var tasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                bool lockTaken = false;
                try
                {
                    // ✅ 正确：lockTaken 初始化为 false，Enter 后检查是否成功
                    _spinLock.Enter(ref lockTaken);
                    _spinCounter++;
                }
                finally
                {
                    // ✅ 只有成功获锁才需要释放
                    if (lockTaken) _spinLock.Exit(useMemoryBarrier: false);
                }
            }
        }));

        Task.WhenAll(tasks).Wait();

        long expected = threads * iterations;
        Console.WriteLine($"  期望值:       {expected:N0}");
        Console.WriteLine($"  SpinLock结果: {_spinCounter:N0} {(_spinCounter == expected ? "✅" : "❌")}");
    }

    // ─── 2. SpinLock vs lock 性能对比 ───────────────────────────────────────

    private static readonly object _normalLock = new();
    private static long _normalCounter = 0;
    private static SpinLock _perfSpinLock = new SpinLock(false);
    private static long _spinPerfCounter = 0;

    /// <summary>
    /// 在极短临界区下对比 lock 和 SpinLock 的性能
    /// 临界区越短，SpinLock 优势越明显（避免线程切换）
    /// </summary>
    public static void SpinLockVsLockPerformance()
    {
        Console.WriteLine("\n── SpinLock vs lock 性能对比（极短临界区）──");

        const int threads = 4;
        const int iterations = 500_000;

        // ── lock 版本
        _normalCounter = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var lockTasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
                lock (_normalLock) { _normalCounter++; }
        }));
        Task.WhenAll(lockTasks).Wait();
        sw.Stop();
        long lockMs = sw.ElapsedMilliseconds;

        // ── SpinLock 版本
        _spinPerfCounter = 0;
        sw.Restart();
        var spinTasks = Enumerable.Range(0, threads).Select(_ => Task.Run(() =>
        {
            for (int i = 0; i < iterations; i++)
            {
                bool taken = false;
                try
                {
                    _perfSpinLock.Enter(ref taken);
                    _spinPerfCounter++;
                }
                finally
                {
                    if (taken) _perfSpinLock.Exit(false);
                }
            }
        }));
        Task.WhenAll(spinTasks).Wait();
        sw.Stop();
        long spinMs = sw.ElapsedMilliseconds;

        Console.WriteLine($"  操作次数: {threads * iterations:N0}");
        Console.WriteLine($"  lock:     {lockMs,6} ms");
        Console.WriteLine($"  SpinLock: {spinMs,6} ms");
        Console.WriteLine($"  注意：临界区越短，SpinLock 优势越大；临界区越长，SpinLock 反而浪费 CPU");
    }

    // ─── 3. SpinLock 常见错误示范 ───────────────────────────────────────────

    /// <summary>
    /// SpinLock 最典型的错误：把 SpinLock 复制给局部变量或方法参数
    /// </summary>
    public static void SpinLockPitfallDemo()
    {
        Console.WriteLine("\n── SpinLock 陷阱：值类型不能复制！──");

        Console.WriteLine("  ❌ 错误做法：");
        Console.WriteLine("     SpinLock myLock = _spinLock;  // 这是复制！两个独立的 SpinLock！");
        Console.WriteLine("     void DoWork(SpinLock sl) { ... }  // 参数传递也是复制！");
        Console.WriteLine();
        Console.WriteLine("  ✅ 正确做法：");
        Console.WriteLine("     // 声明为 static/instance 字段，传递时用 ref");
        Console.WriteLine("     private static SpinLock _lock = new SpinLock(false);");
        Console.WriteLine("     void DoWork(ref SpinLock sl) { ... }  // ref 传递！");
        Console.WriteLine();
        Console.WriteLine("  💡 什么时候用 SpinLock？");
        Console.WriteLine("     ✅ 临界区 < 100ns（纯内存操作，比如更新一个字段）");
        Console.WriteLine("     ✅ 竞争不激烈（大多数时候能立刻获锁）");
        Console.WriteLine("     ❌ 临界区有 IO、数据库、锁套锁 → 用 lock");
        Console.WriteLine("     ❌ 高竞争场景 → 自旋会疯狂消耗 CPU");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  SpinLock 自旋锁演示");
        Console.WriteLine("═══════════════════════════════════════════");

        BasicSpinLockDemo();
        SpinLockVsLockPerformance();
        SpinLockPitfallDemo();
    }
}
