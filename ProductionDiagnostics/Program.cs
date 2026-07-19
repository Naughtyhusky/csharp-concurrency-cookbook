namespace ProductionDiagnostics;

/// <summary>
/// 生产环境诊断实战示例程序
/// 演示死锁、线程池饥饿、内存泄漏、CPU占用等问题的模拟和诊断
/// </summary>
internal class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("=== 生产环境诊断实战示例 ===\n");
        Console.WriteLine("请选择要运行的示例：");
        Console.WriteLine("1. 死锁演示（Deadlock）");
        Console.WriteLine("2. 线程池饥饿演示（ThreadPool Starvation）");
        Console.WriteLine("3. 内存泄漏演示（Memory Leak）");
        Console.WriteLine("4. CPU 占用过高演示（High CPU Usage）");
        Console.WriteLine("5. 异步死锁演示（Async Deadlock）");
        Console.WriteLine("6. 对象池内存泄漏演示（Object Pool Memory Leak）");
        Console.WriteLine("0. 退出\n");

        while (true)
        {
            Console.Write("请输入选项 (0-6): ");
            var input = Console.ReadLine();

            try
            {
                switch (input)
                {
                    case "1":
                        DeadlockDemo.Run();
                        break;
                    case "2":
                        await ThreadPoolStarvationDemo.RunAsync();
                        break;
                    case "3":
                        MemoryLeakDemo.Run();
                        break;
                    case "4":
                        HighCpuDemo.Run();
                        break;
                    case "5":
                        await AsyncDeadlockDemo.RunAsync();
                        break;
                    case "6":
                        ObjectPoolLeakDemo.Run();
                        break;
                    case "0":
                        Console.WriteLine("再见！");
                        return;
                    default:
                        Console.WriteLine("无效的选项，请重新输入。\n");
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n发生错误: {ex.Message}\n");
            }

            Console.WriteLine("\n" + new string('-', 50) + "\n");
        }
    }
}
