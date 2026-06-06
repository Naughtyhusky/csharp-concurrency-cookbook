namespace LockFree;

/// <summary>
/// volatile 关键字与内存可见性示例
/// 演示：没有 volatile 时的指令重排/缓存问题，以及 volatile 的作用
/// </summary>
public static class VolatileDemo
{
    // ---- 示例1：经典的"停止循环"问题 ----
    // 这是 volatile 最典型的使用场景：一个线程写，一个线程读标志位

    private static bool _stopWithoutVolatile = false;
    private static volatile bool _stopWithVolatile = false;

    public static void RunStopLoopDemo()
    {
        Console.WriteLine("=== 示例1：volatile 保证标志位可见性 ===\n");

        // ---- 演示带 volatile 的正确版本 ----
        _stopWithVolatile = false;
        var workerTask = Task.Run(() =>
        {
            int spin = 0;
            // 循环中读取 volatile 变量，保证能看到主线程的写入
            while (!_stopWithVolatile)
            {
                spin++;
                if (spin % 1_000_000 == 0)
                    Console.WriteLine($"  Worker：还在跑... spin={spin:N0}");
            }
            Console.WriteLine("  Worker：收到停止信号，退出循环！");
        });

        Thread.Sleep(50);
        _stopWithVolatile = true; // 主线程写入停止标志
        Console.WriteLine("主线程：已发出停止信号");
        workerTask.Wait(TimeSpan.FromSeconds(2));
        Console.WriteLine();
    }

    // ---- 示例2：Volatile.Read / Volatile.Write ——非 volatile 字段的按需屏障 ----
    // 有时候我们不想把字段声明为 volatile（比如它在结构体里，或者只有部分访问需要屏障）
    // 可以用 Volatile.Read / Volatile.Write 来精确控制

    private static int _flag = 0;
    private static int _data = 0;

    public static void RunVolatileReadWriteDemo()
    {
        Console.WriteLine("=== 示例2：Volatile.Read / Volatile.Write 精确屏障 ===\n");

        // 生产者：先写 data，再写 flag（用 Volatile.Write 保证 data 在 flag 之前对外可见）
        var producer = Task.Run(() =>
        {
            _data = 42; // 准备好数据
            Volatile.Write(ref _flag, 1); // ✅ 保证：_data 的写入不会被重排到这句之后
            Console.WriteLine("  生产者：data=42 已准备好，flag 已设置为 1");
        });

        // 消费者：先读 flag，再读 data（用 Volatile.Read 保证看到最新的 flag）
        var consumer = Task.Run(() =>
        {
            // 等待 flag 变为 1
            int spin = 0;
            while (Volatile.Read(ref _flag) == 0) // ✅ 保证：读到 flag=1 后，_data 的写入已对我可见
            {
                spin++;
                Thread.SpinWait(10);
            }
            Console.WriteLine($"  消费者：flag={_flag}，读到 data={_data}（期望 42），spin次数={spin:N0}");
        });

        Task.WaitAll(producer, consumer);
        Console.WriteLine();
    }

    // ---- 示例3：volatile 的限制 —— 它不能保证复合操作的原子性 ----

    private static volatile int _volatileCounter = 0;

    public static void RunVolatileLimitationDemo()
    {
        Console.WriteLine("=== 示例3：volatile 的局限性（不能保证复合操作原子性）===\n");
        Console.WriteLine("  volatile 只能保证单次读/写的可见性，不能保证 i++ 这类复合操作的原子性");
        Console.WriteLine("  i++ 实际上是 read → add → write 三步，volatile 保不住！");
        Console.WriteLine();

        const int threadCount = 5;
        const int iterations = 100_000;

        // 用 volatile 计数（错误示范）
        _volatileCounter = 0;
        var tasks = Enumerable.Range(0, threadCount)
            .Select(_ => Task.Run(() =>
            {
                for (int i = 0; i < iterations; i++)
                    _volatileCounter++; // ⚠️ volatile 不能保证这个操作的原子性！
            }))
            .ToArray();
        Task.WaitAll(tasks);

        int expected = threadCount * iterations;
        Console.WriteLine($"  [volatile i++] 期望：{expected:N0}，实际：{_volatileCounter:N0}");
        Console.WriteLine($"  丢失了约 {expected - _volatileCounter:N0} 次更新（{(expected - _volatileCounter) * 100.0 / expected:F2}%）");
        Console.WriteLine();
        Console.WriteLine("  ✅ 结论：需要原子性请用 Interlocked，需要可见性用 volatile");
        Console.WriteLine();
    }

    public static void RunAll()
    {
        RunStopLoopDemo();
        RunVolatileReadWriteDemo();
        RunVolatileLimitationDemo();
    }
}
