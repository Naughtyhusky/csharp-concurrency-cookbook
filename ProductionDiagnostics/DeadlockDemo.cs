namespace ProductionDiagnostics;

/// <summary>
/// 死锁演示：模拟经典的死锁场景
/// 诊断工具：dotnet-dump、syncblk、parallelstacks
/// </summary>
public static class DeadlockDemo
{
    private static readonly object _lock1 = new();
    private static readonly object _lock2 = new();

    public static void Run()
    {
        Console.WriteLine("\n=== 死锁演示 ===");
        Console.WriteLine("即将启动两个线程，它们会相互等待对方释放锁，导致死锁...\n");
        Console.WriteLine("⚠️  注意：此程序会卡死！请在另一个终端使用以下命令诊断：");
        Console.WriteLine("1. dotnet-dump collect -p <进程ID>");
        Console.WriteLine("2. dotnet-dump analyze <dump文件>");
        Console.WriteLine("3. 在分析器中执行：clrthreads");
        Console.WriteLine("4. 在分析器中执行：syncblk");
        Console.WriteLine("5. 在分析器中执行：parallelstacks\n");

        Console.WriteLine("按任意键开始死锁演示（程序将卡死）...");
        Console.ReadKey();
        Console.WriteLine();

        // 启动线程 1
        var thread1 = new Thread(() =>
        {
            lock (_lock1)
            {
                Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] 获取了 Lock1");
                Thread.Sleep(1000); // 确保线程2也能获取到lock2

                Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] 尝试获取 Lock2...");
                lock (_lock2) // 等待线程2释放lock2
                {
                    Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] 获取了 Lock2");
                }
            }
        })
        {
            Name = "DeadlockThread1"
        };

        // 启动线程 2
        var thread2 = new Thread(() =>
        {
            lock (_lock2)
            {
                Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] 获取了 Lock2");
                Thread.Sleep(1000); // 确保线程1也能获取到lock1

                Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] 尝试获取 Lock1...");
                lock (_lock1) // 等待线程1释放lock1
                {
                    Console.WriteLine($"[Thread {Environment.CurrentManagedThreadId}] 获取了 Lock1");
                }
            }
        })
        {
            Name = "DeadlockThread2"
        };

        thread1.Start();
        thread2.Start();

        Console.WriteLine("等待 5 秒检测死锁...");
        Thread.Sleep(5000);

        Console.WriteLine("\n❌ 死锁已发生！两个线程都在等待对方释放锁。");
        Console.WriteLine("程序无法继续执行，请使用 Ctrl+C 终止进程。");
        Console.WriteLine("\n💡 预防死锁的方法：");
        Console.WriteLine("1. 避免嵌套锁");
        Console.WriteLine("2. 统一加锁顺序（总是先获取 Lock1，再获取 Lock2）");
        Console.WriteLine("3. 使用超时机制：Monitor.TryEnter(lock, timeout)");
        Console.WriteLine("4. 使用 Semaphore 或 SemaphoreSlim 替代多个锁");

        // 永久等待（模拟死锁）
        thread1.Join();
        thread2.Join();
    }
}
