namespace LockFree;

internal class Program
{
    static void Main(string[] args)
    {
        Console.OutputEncoding = System.Text.Encoding.UTF8;

        Console.WriteLine("╔══════════════════════════════════════════════════════════════╗");
        Console.WriteLine("║       第14章：无锁编程与内存模型 —— 示例代码演示              ║");
        Console.WriteLine("╚══════════════════════════════════════════════════════════════╝");
        Console.WriteLine();

        if (args.Length > 0)
        {
            // 支持命令行指定运行哪个 Demo
            switch (args[0].ToLower())
            {
                case "interlocked": InterlockedDemo.RunAll(); break;
                case "volatile":    VolatileDemo.RunAll();    break;
                case "memory":      MemoryModelDemo.RunAll(); break;
                case "lockfree":    LockFreeStackDemo.RunAll(); break;
                case "lazy":        LazyDemo.RunAll();        break;
                default:            RunAll();                  break;
            }
            return;
        }

        RunAll();
    }

    private static void RunAll()
    {
        PrintSection("Part 1: Interlocked 原子操作");
        InterlockedDemo.RunAll();

        PrintSection("Part 2: volatile 关键字与内存可见性");
        VolatileDemo.RunAll();

        PrintSection("Part 3: .NET 内存模型与内存屏障");
        MemoryModelDemo.RunAll();

        PrintSection("Part 4: 无锁数据结构（Treiber Stack）");
        LockFreeStackDemo.RunAll();

        PrintSection("Part 5: Lazy<T> 与延迟初始化");
        LazyDemo.RunAll();

        Console.WriteLine("════════════════════════════════════════════════════════════════");
        Console.WriteLine("  所有示例运行完毕！");
        Console.WriteLine("════════════════════════════════════════════════════════════════");
    }

    private static void PrintSection(string title)
    {
        Console.WriteLine();
        Console.WriteLine($"────────────────────────────────────────────────────────────────");
        Console.WriteLine($"  {title}");
        Console.WriteLine($"────────────────────────────────────────────────────────────────");
        Console.WriteLine();
    }
}

