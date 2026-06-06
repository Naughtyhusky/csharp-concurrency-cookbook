namespace LockFree;

/// <summary>
/// Interlocked 原子操作示例
/// 演示：Increment/Decrement/Add/CompareExchange/Exchange
/// </summary>
public static class InterlockedDemo
{
    // ---- 示例1：经典的计数器 Race Condition vs Interlocked ----

    private static int _unsafeCounter = 0;
    private static int _safeCounter = 0;

    public static void RunCounterComparison()
    {
        Console.WriteLine("=== 示例1：有锁 vs 无锁计数器对比 ===\n");

        const int threadCount = 10;
        const int iterationsPerThread = 100_000;

        // ---- 不安全版本（没有任何同步）----
        _unsafeCounter = 0;
        var unsafeTasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                    _unsafeCounter++; // ⚠️ 非原子操作：read → add → write，三步，可能丢失更新
            }))
            .ToArray();
        Task.WaitAll(unsafeTasks);
        Console.WriteLine($"[不安全] 期望：{threadCount * iterationsPerThread}，实际：{_unsafeCounter}（可能比期望值小！）");

        // ---- 安全版本（Interlocked）----
        _safeCounter = 0;
        var safeTasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterationsPerThread; i++)
                    Interlocked.Increment(ref _safeCounter); // ✅ 原子操作
            }))
            .ToArray();
        Task.WaitAll(safeTasks);
        Console.WriteLine($"[Interlocked] 期望：{threadCount * iterationsPerThread}，实际：{_safeCounter}（永远正确！）\n");
    }

    // ---- 示例2：CAS（Compare-And-Swap）的核心用法 ----
    // CompareExchange 是无锁编程的基石：
    // "只有当前值等于预期值时，才把它换成新值，整个过程原子完成"

    public static void RunCasDemo()
    {
        Console.WriteLine("=== 示例2：CAS（CompareExchange）原理演示 ===\n");

        int value = 0;

        // 模拟多线程争抢，只让第一个成功的线程把 0 改成 42
        int successCount = 0;
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Task.Run(() =>
            {
                // CompareExchange(ref location, newValue, comparand)
                // 含义：如果 location == comparand，则把 location 设置为 newValue，返回旧值
                int original = Interlocked.CompareExchange(ref value, 42, 0);
                if (original == 0) // 说明我们赢了，是第一个修改的
                {
                    Interlocked.Increment(ref successCount);
                    Console.WriteLine($"  线程 {i}：CAS 成功！旧值={original}，新值=42");
                }
                else
                {
                    Console.WriteLine($"  线程 {i}：CAS 失败，当前值已是 {original}，放弃修改");
                }
            }))
            .ToArray();
        Task.WaitAll(tasks);
        Console.WriteLine($"\n最终值：{value}，成功修改次数：{successCount}（只会有 1 次！）\n");
    }

    // ---- 示例3：用 CAS 实现一个无锁的"最大值更新" ----
    // 这个模式非常实用，面试也常考

    private static int _maxValue = 0;

    public static void RunCasMaxValueDemo()
    {
        Console.WriteLine("=== 示例3：CAS 实现无锁最大值更新 ===\n");

        _maxValue = 0;
        int[] numbers = [5, 12, 3, 99, 7, 45, 99, 22, 88, 1];

        var tasks = numbers.Select(n => Task.Run(() => TryUpdateMax(n))).ToArray();
        Task.WaitAll(tasks);

        Console.WriteLine($"输入数组：[{string.Join(", ", numbers)}]");
        Console.WriteLine($"无锁最大值结果：{_maxValue}（正确答案：{numbers.Max()}）\n");
    }

    private static void TryUpdateMax(int newValue)
    {
        // 经典 CAS 循环（Spin）：尝试更新，失败就重试
        while (true)
        {
            int current = _maxValue;
            if (newValue <= current) return; // 新值不比当前大，不需要更新

            // 尝试把 current 替换成 newValue
            // 如果此时 _maxValue 还是 current，说明没有其他线程抢先，我们成功
            int original = Interlocked.CompareExchange(ref _maxValue, newValue, current);
            if (original == current) return; // 成功！退出循环

            // 走到这里说明有其他线程抢先改了 _maxValue，重新读取再试一次
        }
    }

    // ---- 示例4：Exchange —— 原子读写 ----

    public static void RunExchangeDemo()
    {
        Console.WriteLine("=== 示例4：Exchange 原子读写 ===\n");

        int sharedFlag = 0;

        // 模拟"只有一个线程能拿到令牌"
        var tasks = Enumerable.Range(0, 5)
            .Select(i => Task.Run(() =>
            {
                // Exchange 把新值写入，同时返回旧值，整个操作原子完成
                int old = Interlocked.Exchange(ref sharedFlag, 1);
                if (old == 0)
                    Console.WriteLine($"  线程 {i}：获得令牌（old={old}）");
                else
                    Console.WriteLine($"  线程 {i}：令牌已被占用（old={old}）");
            }))
            .ToArray();
        Task.WaitAll(tasks);
        Console.WriteLine();
    }

    // ---- 示例5：Interlocked.Add 和 Read（64位原子读）----

    public static void RunAddAndReadDemo()
    {
        Console.WriteLine("=== 示例5：Interlocked.Add 与 64位原子读 ===\n");

        long total = 0;
        const int count = 1_000_000;

        var tasks = Enumerable.Range(0, 4)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < count; i++)
                    Interlocked.Add(ref total, 1L); // long 版本
            }))
            .ToArray();
        Task.WaitAll(tasks);

        // 在 32 位环境下，直接读取 long 不是原子操作，要用 Read 保证
        long result = Interlocked.Read(ref total);
        Console.WriteLine($"4个线程各加 {count:N0} 次，期望总和：{4L * count:N0}，实际：{result:N0}\n");
    }

    public static void RunAll()
    {
        RunCounterComparison();
        RunCasDemo();
        RunCasMaxValueDemo();
        RunExchangeDemo();
        RunAddAndReadDemo();
    }
}
