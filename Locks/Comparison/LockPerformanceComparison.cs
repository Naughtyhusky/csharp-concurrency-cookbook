namespace Locks.Comparison;

/// <summary>
/// 各种锁的性能横向对比
/// 帮助你建立"选哪个锁"的直觉
/// </summary>
public static class LockPerformanceComparison
{
    private const int Iterations = 200_000;
    private const int Threads = 4;

    public static async Task RunAllComparisons()
    {
        Console.WriteLine("\n── 各种锁性能横向对比 ──");
        Console.WriteLine($"  {Threads} 线程 × {Iterations:N0} 次累加 = {Threads * Iterations:N0} 次操作");
        Console.WriteLine();

        var results = new List<(string Name, long Ticks, bool Correct)>();

        // ── 1. Interlocked（无锁，最快基准）
        {
            long counter = 0;
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < Iterations; i++)
                    Interlocked.Increment(ref counter);
            })));
            sw.Stop();
            results.Add(("Interlocked（无锁基准）", sw.ElapsedTicks, counter == Threads * Iterations));
        }

        // ── 2. lock / Monitor
        {
            long counter = 0;
            var lockObj = new object();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < Iterations; i++)
                    lock (lockObj) { counter++; }
            })));
            sw.Stop();
            results.Add(("lock / Monitor", sw.ElapsedTicks, counter == Threads * Iterations));
        }

        // ── 3. SpinLock
        {
            long counter = 0;
            var spinLock = new SpinLock(false);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    bool taken = false;
                    try { spinLock.Enter(ref taken); counter++; }
                    finally { if (taken) spinLock.Exit(false); }
                }
            })));
            sw.Stop();
            results.Add(("SpinLock", sw.ElapsedTicks, counter == Threads * Iterations));
        }

        // ── 4. SemaphoreSlim（同步 Wait）
        {
            long counter = 0;
            using var sem = new SemaphoreSlim(1, 1);
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    sem.Wait();
                    try { counter++; }
                    finally { sem.Release(); }
                }
            })));
            sw.Stop();
            results.Add(("SemaphoreSlim（同步 Wait）", sw.ElapsedTicks, counter == Threads * Iterations));
        }

        // ── 5. ReaderWriterLockSlim（写锁）
        {
            long counter = 0;
            using var rwLock = new ReaderWriterLockSlim();
            var sw = System.Diagnostics.Stopwatch.StartNew();
            await Task.WhenAll(Enumerable.Range(0, Threads).Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < Iterations; i++)
                {
                    rwLock.EnterWriteLock();
                    try { counter++; }
                    finally { rwLock.ExitWriteLock(); }
                }
            })));
            sw.Stop();
            results.Add(("ReaderWriterLockSlim（写锁）", sw.ElapsedTicks, counter == Threads * Iterations));
        }

        // 输出结果表
        long baseline = results[0].Ticks;
        Console.WriteLine($"  {"锁类型",-35} {"Ticks",12} {"倍数（vs无锁）",14} {"结果"}");
        Console.WriteLine($"  {"─".PadRight(80, '─')}");
        foreach (var (name, ticks, correct) in results)
        {
            double ratio = baseline > 0 ? (double)ticks / baseline : 0;
            Console.WriteLine($"  {name,-35} {ticks,12:N0} {ratio,12:F1}x  {(correct ? "✅" : "❌")}");
        }

        Console.WriteLine();
        Console.WriteLine("  📝 解读：");
        Console.WriteLine("     - Interlocked：原子指令，无锁开销，是计数器的最优选择");
        Console.WriteLine("     - SpinLock：比 lock 快，但高竞争下反而浪费 CPU");
        Console.WriteLine("     - lock：综合最佳，绝大多数场景的首选");
        Console.WriteLine("     - SemaphoreSlim：稍慢于 lock，但支持 async/await，异步场景必选");
        Console.WriteLine("     - ReaderWriterLockSlim：读多写少时，读操作可并行，整体更快");
    }

    public static async Task Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  锁性能横向对比");
        Console.WriteLine("═══════════════════════════════════════════");

        await RunAllComparisons();
    }
}
