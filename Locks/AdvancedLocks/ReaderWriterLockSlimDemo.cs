namespace Locks.AdvancedLocks;

/// <summary>
/// ReaderWriterLockSlim：读写锁
/// 核心思想：读读并行，读写互斥，写写互斥
/// 适用场景：读多写少（配置、缓存、字典查询等）
/// </summary>
public static class ReaderWriterLockSlimDemo
{
    // ─── 线程安全缓存：多读单写 ───────────────────────────────────────────────

    public static class ThreadSafeCache
    {
        private static readonly ReaderWriterLockSlim _lock = new();
        private static readonly Dictionary<string, string> _store = new();

        public static string? Get(string key)
        {
            _lock.EnterReadLock();
            try
            {
                _store.TryGetValue(key, out var val);
                return val;
            }
            finally { _lock.ExitReadLock(); }
        }

        public static void Set(string key, string value)
        {
            _lock.EnterWriteLock();
            try { _store[key] = value; }
            finally { _lock.ExitWriteLock(); }
        }

        /// <summary>
        /// 升级锁：先读，不存在才升级为写
        /// 比"释放读锁再获写锁"更安全，没有竞态窗口
        /// </summary>
        public static string GetOrAdd(string key, Func<string> valueFactory)
        {
            _lock.EnterUpgradeableReadLock();
            try
            {
                if (_store.TryGetValue(key, out var existing))
                    return existing;

                _lock.EnterWriteLock();
                try
                {
                    if (!_store.TryGetValue(key, out existing))
                    {
                        existing = valueFactory();
                        _store[key] = existing;
                    }
                    return existing;
                }
                finally { _lock.ExitWriteLock(); }
            }
            finally { _lock.ExitUpgradeableReadLock(); }
        }

        public static int Count
        {
            get
            {
                _lock.EnterReadLock();
                try { return _store.Count; }
                finally { _lock.ExitReadLock(); }
            }
        }
    }

    public static void ReadWriteConcurrencyDemo()
    {
        Console.WriteLine("\n── ReaderWriterLockSlim 读多写少演示 ──");

        for (int i = 0; i < 10; i++)
            ThreadSafeCache.Set($"key{i}", $"value{i}");

        int readCount = 0, writeCount = 0;
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var tasks = new List<Task>();

        for (int i = 0; i < 8; i++)
        {
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 200; j++)
                {
                    ThreadSafeCache.Get($"key{j % 10}");
                    Interlocked.Increment(ref readCount);
                    Thread.Sleep(1);
                }
            }));
        }

        for (int i = 0; i < 2; i++)
        {
            int writerId = i;
            tasks.Add(Task.Run(() =>
            {
                for (int j = 0; j < 20; j++)
                {
                    ThreadSafeCache.Set($"key{j % 10}", $"w{writerId}_{j}");
                    Interlocked.Increment(ref writeCount);
                    Thread.Sleep(10);
                }
            }));
        }

        Task.WhenAll(tasks).Wait();
        sw.Stop();

        Console.WriteLine($"  完成: {readCount} 次读 + {writeCount} 次写，耗时 {sw.ElapsedMilliseconds} ms");
        Console.WriteLine("  多个读线程可同时持有读锁，只有写操作才独占");
    }

    public static void UpgradeableLockDemo()
    {
        Console.WriteLine("\n── 可升级读锁（GetOrAdd 场景）──");

        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(() =>
        {
            var val = ThreadSafeCache.GetOrAdd($"lazy_{i % 3}", () =>
            {
                Thread.Sleep(5);
                return $"computed_{i % 3}";
            });
            Console.WriteLine($"  [Thread {Thread.CurrentThread.ManagedThreadId,2}] GetOrAdd(lazy_{i % 3}) = {val}");
        }));

        Task.WhenAll(tasks).Wait();
    }

    public static void PitfallDemo()
    {
        Console.WriteLine("\n── ⚠️ 常见陷阱 ──");
        Console.WriteLine("  ❌ 忘记 Dispose：ReaderWriterLockSlim 实现了 IDisposable");
        Console.WriteLine("  ❌ 递归进入：默认 LockRecursionPolicy.NoRecursion，同线程重入会死锁");
        Console.WriteLine("  ✅ 如需递归：new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion)");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  ReaderWriterLockSlim 读写锁演示");
        Console.WriteLine("═══════════════════════════════════════════");

        ReadWriteConcurrencyDemo();
        UpgradeableLockDemo();
        PitfallDemo();
    }
}
