namespace LockFree;

/// <summary>
/// .NET 内存模型与内存屏障示例
/// 演示：指令重排的概念、Thread.MemoryBarrier 的用法
/// </summary>
public static class MemoryModelDemo
{
    // ---- 示例1：内存屏障（Memory Barrier）的使用 ----
    // Thread.MemoryBarrier() 相当于一道"围栏"：
    // 屏障前的读写操作，不能被重排到屏障之后；屏障后的，不能被重排到之前。

    private static int _x = 0;
    private static int _y = 0;
    private static int _r1 = 0;
    private static int _r2 = 0;

    public static void RunMemoryBarrierDemo()
    {
        Console.WriteLine("=== 示例1：Thread.MemoryBarrier 防止指令重排 ===\n");
        Console.WriteLine("  经典的 StoreLoad 重排场景（理论演示）：");
        Console.WriteLine("  线程A：x=1; 屏障; r1=y");
        Console.WriteLine("  线程B：y=1; 屏障; r2=x");
        Console.WriteLine("  没有屏障时，理论上可能出现 r1=0 且 r2=0 的情况（x86 上极罕见，ARM 上可能）");
        Console.WriteLine("  加上屏障后，保证了写入对其他核心可见\n");

        int noReorderCount = 0;
        int totalRounds = 100_000;

        for (int round = 0; round < totalRounds; round++)
        {
            _x = 0;
            _y = 0;

            var t1 = new Thread(() =>
            {
                _x = 1;
                Thread.MemoryBarrier(); // ← 保证 _x=1 在读 _y 之前对其他核心可见
                _r1 = _y;
            });

            var t2 = new Thread(() =>
            {
                _y = 1;
                Thread.MemoryBarrier(); // ← 保证 _y=1 在读 _x 之前对其他核心可见
                _r2 = _x;
            });

            t1.Start();
            t2.Start();
            t1.Join();
            t2.Join();

            // 有了屏障，r1=0 且 r2=0 不应该同时发生
            if (!(_r1 == 0 && _r2 == 0))
                noReorderCount++;
        }

        Console.WriteLine($"  执行 {totalRounds:N0} 轮：{noReorderCount:N0} 轮结果正常，{totalRounds - noReorderCount:N0} 轮出现了 r1=r2=0");
        Console.WriteLine($"  （加了 MemoryBarrier 后，r1=r2=0 极少发生甚至不发生）\n");
    }

    // ---- 示例2：演示 .NET 内存模型的"获取-释放语义" ----
    // Interlocked 和 volatile 都隐含了 Acquire/Release 语义
    // Acquire（读屏障）：之后的读写不能重排到之前
    // Release（写屏障）：之前的读写不能重排到之后

    private static int _dataReady = 0;
    private static string _message = string.Empty;

    public static void RunAcquireReleaseDemo()
    {
        Console.WriteLine("=== 示例2：获取-释放语义（Acquire/Release）===\n");

        var producer = Task.Run(() =>
        {
            _message = "Hello from producer!"; // 先写数据
            // Interlocked.Exchange 包含 Release 语义（写屏障）
            // 保证：_message 的写入不会被重排到这句之后
            Interlocked.Exchange(ref _dataReady, 1);
            Console.WriteLine("  生产者：消息已写入，标志已设置");
        });

        var consumer = Task.Run(() =>
        {
            // Interlocked.CompareExchange 包含 Acquire 语义（读屏障）
            // 保证：_message 的读取不会被重排到这句之前
            while (Interlocked.CompareExchange(ref _dataReady, 0, 0) == 0)
                Thread.SpinWait(1);

            Console.WriteLine($"  消费者：读到消息 = \"{_message}\"（保证能读到最新值）");
        });

        Task.WaitAll(producer, consumer);
        Console.WriteLine();
    }

    // ---- 示例3：什么时候需要考虑内存模型 ----

    public static void RunWhenToCareDEmo()
    {
        Console.WriteLine("=== 示例3：什么时候需要关心内存模型 ===\n");
        Console.WriteLine("  💡 实际上，大多数 .NET 开发者不需要手动处理内存屏障，因为：");
        Console.WriteLine("     1. lock 语句自带完整的内存屏障（Acquire on enter, Release on exit）");
        Console.WriteLine("     2. Task.Run / await 的调度也隐含了必要的内存同步");
        Console.WriteLine("     3. volatile 和 Interlocked 已经封装了平台相关的屏障");
        Console.WriteLine();
        Console.WriteLine("  ⚠️  需要手动考虑内存模型的场景：");
        Console.WriteLine("     1. 实现无锁数据结构（如无锁栈、队列）");
        Console.WriteLine("     2. 编写高性能框架底层代码");
        Console.WriteLine("     3. 针对 ARM 架构做跨平台优化（ARM 内存模型比 x86 宽松得多）");
        Console.WriteLine("     4. 自定义 SpinLock / Semaphore 实现");
        Console.WriteLine();
        Console.WriteLine("  📋 ARM vs x86 内存模型简单对比：");
        Console.WriteLine("     x86/x64：强内存模型，Store-Load 以外的重排基本不会发生");
        Console.WriteLine("     ARM/ARM64：弱内存模型，几乎所有类型的重排都可能发生，需要更多屏障");
        Console.WriteLine("     .NET 在不同平台上会自动插入相应的屏障指令（MFENCE/DMB等）");
        Console.WriteLine();
    }

    public static void RunAll()
    {
        RunMemoryBarrierDemo();
        RunAcquireReleaseDemo();
        RunWhenToCareDEmo();
    }
}
