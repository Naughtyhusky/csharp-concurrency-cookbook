namespace Locks.Internals;

/// <summary>
/// 锁的底层原理：用户态 vs 内核态 vs 自旋
/// 理解这三种机制，才能知道为什么选不同的锁
/// </summary>
public static class LockInternalsDemo
{
    public static void UserModeVsKernelModeDemo()
    {
        Console.WriteLine("\n── 锁的底层原理：用户态 vs 内核态 ──");

        Console.WriteLine("""
          三种同步机制的本质：

          1. 【用户态锁】lock / Monitor / SpinLock
             原理：CPU 原子指令（如 CMPXCHG）在用户空间完成比较-交换
             特点：不需要操作系统介入，纯用户空间操作
             开销：~10-30 ns（纯 CPU 时间）
             适用：绝大多数场景，临界区从纳秒到毫秒都可以

          2. 【内核态锁】Mutex / Semaphore（非 Slim）/ EventWaitHandle
             原理：调用 Windows 内核对象，需要用户态 → 内核态切换
             特点：跨进程可见，OS 管理生命周期
             开销：~1000-5000 ns（上下文切换）
             适用：跨进程互斥（单实例程序），跨 AppDomain 同步

          3. 【混合锁】Monitor（升级策略）
             原理：先自旋（用户态），一定次数后进入内核等待
             特点：综合了自旋的快速和内核等待的省 CPU
             这正是 lock 背后 Monitor 的真正策略！

          ┌──────────────────────────────────────────────────────────────┐
          │    锁类型          │ 机制    │ 延迟      │ 跨进程 │ 异步支持 │
          ├──────────────────────────────────────────────────────────────┤
          │    lock / Monitor  │ 混合    │ ~30 ns    │  ❌    │   ❌     │
          │    SpinLock        │ 自旋    │ ~10 ns    │  ❌    │   ❌     │
          │    ReaderWriterLS  │ 混合    │ ~50 ns    │  ❌    │   ❌     │
          │    SemaphoreSlim   │ 混合    │ ~100 ns   │  ❌    │   ✅     │
          │    Mutex           │ 内核    │ ~1000 ns  │  ✅    │   ❌     │
          └──────────────────────────────────────────────────────────────┘
        """);
    }

    public static void SpinWaitDemo()
    {
        Console.WriteLine("\n── SpinWait：比 SpinLock 更底层的自旋工具 ──");

        Console.WriteLine("  SpinWait 是比 SpinLock 更底层的自旋原语，自动管理自旋策略：");
        Console.WriteLine("  - 前几次：Thread.SpinWait（CPU 级自旋，最快）");
        Console.WriteLine("  - 之后：Thread.Yield（让出 CPU 时间片）");
        Console.WriteLine("  - 最终：Thread.Sleep(0) / Thread.Sleep(1)（真正休眠）");
        Console.WriteLine();

        // 典型用法：等待某个条件成立
        // volatile 不能用于局部变量，改为通过 Volatile.Read/Write 访问
        bool ready = false;

        var producer = Task.Run(async () =>
        {
            await Task.Delay(50);
            Volatile.Write(ref ready, true);
            Console.WriteLine("  [生产者] 设置 ready = true");
        });

        var consumer = Task.Run(() =>
        {
            var spinWait = new SpinWait();
            while (!Volatile.Read(ref ready))
            {
                spinWait.SpinOnce();
            }
            Console.WriteLine("  [消费者] 检测到 ready，开始处理");
        });

        Task.WhenAll(producer, consumer).Wait();
        Console.WriteLine("  SpinWait 的核心价值：自适应自旋策略，短暂等待时极高效，长等待自动让步");
    }

    public static void Demo()
    {
        Console.WriteLine("═══════════════════════════════════════════");
        Console.WriteLine("  锁底层原理解析");
        Console.WriteLine("═══════════════════════════════════════════");

        UserModeVsKernelModeDemo();
        SpinWaitDemo();
    }
}
